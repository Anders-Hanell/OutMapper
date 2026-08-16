using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Text.Json;

using Algorithms;
using DataStructures;
using Messages;

namespace TaskManager;

internal readonly record struct PersistedGraphData(
    bool Found,
    string? ChannelAName,
    string? ChannelBName,
    ImmutableArray<double> ChannelABinEdges,
    ImmutableArray<double> ChannelBBinEdges,
    ImmutableArray<string> CellColorsRowMajor);

internal static class AnalysisService
{
    private const string GenerationResultFileName = "generation-result.json";
    private const string GraphDataFileName = "graph-data.json";
    private const double ColorScaleMinValue = -0.1;
    private const double ColorScaleMaxValue = 0.1;

    internal static async Task<GenerateAnalysisGraphResponse> GenerateGraphAsync(
        IFileSystem fileSystem, string? projectFolder, string analysisName, TwoVariableAnalysisSettings settings)
    {
        if (string.IsNullOrWhiteSpace(projectFolder))
        {
            return PersistAndReturn(fileSystem, projectFolder, analysisName, settings, "No project was specified.", 0, 0, 0, 0);
        }

        var cohortFolder = Path.Combine(projectFolder, "OutMapper_InternalFiles", "Cohorts", settings.CohortName);
        var cohortDataFile = Path.Combine(cohortFolder, "Parsed data", "cohort.json");

        if (!fileSystem.FileExists(cohortDataFile))
        {
            return PersistAndReturn(
                fileSystem, projectFolder, analysisName, settings,
                $"Cohort '{settings.CohortName}' has not been parsed yet.", 0, 0, 0, 0);
        }

        Cohort cohort;
        try
        {
            var cohortBytes = (await fileSystem.ReadAllBytesAsync(cohortDataFile)).ToList();
            switch (Cohort.FromByteArray(cohortBytes))
            {
                case Success<Cohort> success:
                    cohort = success.Value;
                    break;
                case Failure<Cohort> failure:
                    return PersistAndReturn(fileSystem, projectFolder, analysisName, settings, failure.Error, 0, 0, 0, 0);
                default:
                    return PersistAndReturn(fileSystem, projectFolder, analysisName, settings, "Could not read cohort data.", 0, 0, 0, 0);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return PersistAndReturn(
                fileSystem, projectFolder, analysisName, settings, $"Could not read cohort data: {exception.Message}", 0, 0, 0, 0);
        }

        var totalPatientCount = cohort.PatientIds.Length;
        var linkedDatasetsFile = Path.Combine(cohortFolder, "linked-datasets.json");
        var linkedDatasetNames = Array.Empty<string>();
        if (fileSystem.FileExists(linkedDatasetsFile))
        {
            try
            {
                linkedDatasetNames =
                    JsonSerializer.Deserialize<string[]>(await fileSystem.ReadAllBytesAsync(linkedDatasetsFile))
                    ?? Array.Empty<string>();
            }
            catch (JsonException)
            {
                linkedDatasetNames = Array.Empty<string>();
            }
        }

        if (linkedDatasetNames.Length == 0)
        {
            return PersistAndReturn(
                fileSystem, projectFolder, analysisName, settings,
                $"Cohort '{settings.CohortName}' is not linked to any dataset.", totalPatientCount, 0, 0, 0);
        }

        var datasetsFolder = Path.Combine(projectFolder, "OutMapper_InternalFiles", "Datasets");

        // Patients whose parsed time series was found in exactly one linked dataset, whose outcome
        // parses as a number, and whose time series has both configured channels. "All data should be
        // included" means no further filtering happens here beyond what's needed to compute a value.
        var candidateSeries = new List<TimeSeries>();
        var candidateOutcomes = new List<double>();
        var unmatchedCount = 0;
        var ambiguousCount = 0;

        for (var i = 0; i < cohort.PatientIds.Length; i++)
        {
            var patientId = cohort.PatientIds[i];
            var matchingFiles = new List<string>();
            foreach (var datasetName in linkedDatasetNames)
            {
                var candidateFile = Path.Combine(datasetsFolder, datasetName, "Parsed data", patientId + ".json");
                if (fileSystem.FileExists(candidateFile))
                {
                    matchingFiles.Add(candidateFile);
                }
            }

            if (matchingFiles.Count == 0)
            {
                unmatchedCount++;
                continue;
            }

            if (matchingFiles.Count > 1)
            {
                ambiguousCount++;
                continue;
            }

            if (!double.TryParse(cohort.Outcomes[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var outcomeValue))
            {
                // Not separately itemized in this minimal pass; the patient is simply excluded.
                continue;
            }

            List<byte> timeSeriesBytes;
            try
            {
                timeSeriesBytes = (await fileSystem.ReadAllBytesAsync(matchingFiles[0])).ToList();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            if (TimeSeries.FromByteArray(timeSeriesBytes) is not Success<TimeSeries> parsedSeries)
            {
                continue;
            }

            if (!parsedSeries.Value.Channels.ContainsKey(settings.ChannelAName) ||
                !parsedSeries.Value.Channels.ContainsKey(settings.ChannelBName))
            {
                continue;
            }

            candidateSeries.Add(parsedSeries.Value);
            candidateOutcomes.Add(outcomeValue);
        }

        var channelAMin = double.PositiveInfinity;
        var channelAMax = double.NegativeInfinity;
        var channelBMin = double.PositiveInfinity;
        var channelBMax = double.NegativeInfinity;

        foreach (var series in candidateSeries)
        {
            AccumulateRange(series.Channels[settings.ChannelAName], TimeSeries.MissingValue, ref channelAMin, ref channelAMax);
            AccumulateRange(series.Channels[settings.ChannelBName], TimeSeries.MissingValue, ref channelBMin, ref channelBMax);
        }

        if (double.IsPositiveInfinity(channelAMin) || double.IsPositiveInfinity(channelBMin))
        {
            return PersistAndReturn(
                fileSystem, projectFolder, analysisName, settings,
                "No valid data was found for the configured channels among the cohort's matched patients.",
                totalPatientCount, 0, unmatchedCount, ambiguousCount);
        }

        var channelABinEdges = GridBinning.ComputeBinEdges(channelAMin, channelAMax, settings.ChannelABinSize);
        var channelBBinEdges = GridBinning.ComputeBinEdges(channelBMin, channelBMax, settings.ChannelBBinSize);
        var rowCount = channelBBinEdges.Length - 1;
        var colCount = channelABinEdges.Length - 1;

        var matchedMatrices = new List<double[,]>();
        var matchedOutcomes = new List<double>();

        for (var i = 0; i < candidateSeries.Count; i++)
        {
            var series = candidateSeries[i];
            var percentMatrix = PercentTimeGrid.ComputePercentTimeMatrix(
                series.Channels[settings.ChannelAName],
                series.Channels[settings.ChannelBName],
                channelABinEdges,
                channelBBinEdges,
                TimeSeries.MissingValue);

            if (percentMatrix is null)
            {
                continue;
            }

            matchedMatrices.Add(percentMatrix);
            matchedOutcomes.Add(candidateOutcomes[i]);
        }

        var associationGrid = AssociationGrid.Compute(matchedMatrices, matchedOutcomes, rowCount, colCount);

        var cellColors = new string[rowCount * colCount];
        for (var row = 0; row < rowCount; row++)
        {
            for (var col = 0; col < colCount; col++)
            {
                cellColors[row * colCount + col] =
                    JetColorScale.ToHexColor(associationGrid[row, col], ColorScaleMinValue, ColorScaleMaxValue) ?? string.Empty;
            }
        }

        var response = new GenerateAnalysisGraphResponse(
            projectFolder,
            analysisName,
            Success: true,
            ErrorMessage: null,
            settings.CohortName,
            settings.ChannelAName,
            settings.ChannelBName,
            totalPatientCount,
            matchedMatrices.Count,
            unmatchedCount,
            ambiguousCount,
            channelABinEdges.ToImmutableArray(),
            channelBBinEdges.ToImmutableArray(),
            cellColors.ToImmutableArray());

        WriteSummary(fileSystem, projectFolder, analysisName, response);
        WriteGraphData(fileSystem, projectFolder, analysisName, response);
        return response;
    }

    internal static AnalysisResultResponse ReadPersistedResult(
        IFileSystem fileSystem, string? projectFolder, string analysisName)
    {
        if (string.IsNullOrWhiteSpace(projectFolder))
        {
            return NoGenerationHasRun(projectFolder ?? string.Empty, analysisName);
        }

        var summaryFilePath = ResolveSummaryFilePath(projectFolder, analysisName);
        if (!fileSystem.FileExists(summaryFilePath))
        {
            return NoGenerationHasRun(projectFolder, analysisName);
        }

        try
        {
            var dto = JsonSerializer.Deserialize<GenerationResultSummaryDto>(fileSystem.ReadAllBytes(summaryFilePath));
            if (dto is null)
            {
                return NoGenerationHasRun(projectFolder, analysisName);
            }

            return new AnalysisResultResponse(
                projectFolder,
                dto.AnalysisName,
                GenerationHasRun: true,
                dto.GeneratedAtUtc,
                dto.Success,
                dto.ErrorMessage,
                dto.CohortName,
                dto.ChannelAName,
                dto.ChannelBName,
                dto.MatchedPatientCount,
                dto.TotalPatientCount);
        }
        catch (JsonException)
        {
            return NoGenerationHasRun(projectFolder, analysisName);
        }
    }

    private static void AccumulateRange(
        ImmutableArray<float> values, float missingValue, ref double min, ref double max)
    {
        foreach (var value in values)
        {
            if (value == missingValue)
            {
                continue;
            }

            if (value < min)
            {
                min = value;
            }

            if (value > max)
            {
                max = value;
            }
        }
    }

    private static GenerateAnalysisGraphResponse PersistAndReturn(
        IFileSystem fileSystem, string? projectFolder, string analysisName, TwoVariableAnalysisSettings settings,
        string errorMessage, int totalPatientCount, int matchedPatientCount, int unmatchedPatientCount, int ambiguousPatientCount)
    {
        var response = new GenerateAnalysisGraphResponse(
            projectFolder ?? string.Empty,
            analysisName,
            Success: false,
            errorMessage,
            settings.CohortName,
            settings.ChannelAName,
            settings.ChannelBName,
            totalPatientCount,
            matchedPatientCount,
            unmatchedPatientCount,
            ambiguousPatientCount,
            ImmutableArray<double>.Empty,
            ImmutableArray<double>.Empty,
            ImmutableArray<string>.Empty);

        if (!string.IsNullOrWhiteSpace(projectFolder))
        {
            WriteSummary(fileSystem, projectFolder, analysisName, response);
        }

        return response;
    }

    private static AnalysisResultResponse NoGenerationHasRun(string projectFolder, string analysisName)
    {
        return new AnalysisResultResponse(
            projectFolder, analysisName, GenerationHasRun: false, GeneratedAtUtc: null,
            Success: false, ErrorMessage: null, CohortName: null, ChannelAName: null, ChannelBName: null,
            MatchedPatientCount: 0, TotalPatientCount: 0);
    }

    private static void WriteSummary(
        IFileSystem fileSystem, string projectFolder, string analysisName, GenerateAnalysisGraphResponse response)
    {
        try
        {
            var dto = new GenerationResultSummaryDto
            {
                AnalysisName = response.AnalysisName,
                GeneratedAtUtc = DateTime.UtcNow,
                Success = response.Success,
                ErrorMessage = response.ErrorMessage,
                CohortName = response.CohortName,
                ChannelAName = response.ChannelAName,
                ChannelBName = response.ChannelBName,
                MatchedPatientCount = response.MatchedPatientCount,
                TotalPatientCount = response.TotalPatientCount
            };

            var summaryFilePath = ResolveSummaryFilePath(projectFolder, analysisName);
            fileSystem.CreateDirectory(Path.GetDirectoryName(summaryFilePath)!);
            fileSystem.WriteAllBytes(summaryFilePath, JsonSerializer.SerializeToUtf8Bytes(dto));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // The response already carries the original outcome; a failure to persist it is not itself fatal.
        }
    }

    private static string ResolveSummaryFilePath(string projectFolder, string analysisName)
    {
        return Path.Combine(projectFolder, "OutMapper_InternalFiles", "Analyses", analysisName, GenerationResultFileName);
    }

    private sealed class GenerationResultSummaryDto
    {
        public string AnalysisName { get; set; } = "";
        public DateTime GeneratedAtUtc { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? CohortName { get; set; }
        public string? ChannelAName { get; set; }
        public string? ChannelBName { get; set; }
        public int MatchedPatientCount { get; set; }
        public int TotalPatientCount { get; set; }
    }

    private static void WriteGraphData(
        IFileSystem fileSystem, string projectFolder, string analysisName, GenerateAnalysisGraphResponse response)
    {
        try
        {
            var dto = new GraphDataDto
            {
                ChannelAName = response.ChannelAName,
                ChannelBName = response.ChannelBName,
                ChannelABinEdges = response.ChannelABinEdges.ToArray(),
                ChannelBBinEdges = response.ChannelBBinEdges.ToArray(),
                CellColorsRowMajor = response.CellColorsRowMajor.ToArray()
            };

            var graphDataFilePath = ResolveGraphDataFilePath(projectFolder, analysisName);
            fileSystem.CreateDirectory(Path.GetDirectoryName(graphDataFilePath)!);
            fileSystem.WriteAllBytes(graphDataFilePath, JsonSerializer.SerializeToUtf8Bytes(dto));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // The response already carries the original outcome; a failure to persist it is not itself fatal.
        }
    }

    internal static PersistedGraphData ReadPersistedGraphData(
        IFileSystem fileSystem, string? projectFolder, string analysisName)
    {
        if (string.IsNullOrWhiteSpace(projectFolder))
        {
            return default;
        }

        var graphDataFilePath = ResolveGraphDataFilePath(projectFolder, analysisName);
        if (!fileSystem.FileExists(graphDataFilePath))
        {
            return default;
        }

        try
        {
            var dto = JsonSerializer.Deserialize<GraphDataDto>(fileSystem.ReadAllBytes(graphDataFilePath));
            if (dto is null)
            {
                return default;
            }

            return new PersistedGraphData(
                Found: true,
                dto.ChannelAName,
                dto.ChannelBName,
                dto.ChannelABinEdges.ToImmutableArray(),
                dto.ChannelBBinEdges.ToImmutableArray(),
                dto.CellColorsRowMajor.ToImmutableArray());
        }
        catch (JsonException)
        {
            return default;
        }
    }

    internal static ImmutableArray<string> ListAnalysesWithPersistedGraph(
        IFileSystem fileSystem, string? projectFolder)
    {
        if (string.IsNullOrWhiteSpace(projectFolder) || !fileSystem.DirectoryExists(projectFolder))
        {
            return ImmutableArray<string>.Empty;
        }

        var analysesFolder = Path.Combine(projectFolder, "OutMapper_InternalFiles", "Analyses");
        if (!fileSystem.DirectoryExists(analysesFolder))
        {
            return ImmutableArray<string>.Empty;
        }

        var analysisNames = fileSystem.GetFiles(analysesFolder, "*.oman")
            .Select(file => Path.GetFileNameWithoutExtension(file)!)
            .Where(name => !string.IsNullOrWhiteSpace(name));

        return analysisNames
            .Where(name => ReadPersistedGraphData(fileSystem, projectFolder, name).Found)
            .ToImmutableArray();
    }

    private static string ResolveGraphDataFilePath(string projectFolder, string analysisName)
    {
        return Path.Combine(projectFolder, "OutMapper_InternalFiles", "Analyses", analysisName, GraphDataFileName);
    }

    private sealed class GraphDataDto
    {
        public string? ChannelAName { get; set; }
        public string? ChannelBName { get; set; }
        public double[] ChannelABinEdges { get; set; } = [];
        public double[] ChannelBBinEdges { get; set; } = [];
        public string[] CellColorsRowMajor { get; set; } = [];
    }
}
