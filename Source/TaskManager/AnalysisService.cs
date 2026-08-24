using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Text.Json;

using Algorithms;
using DataStructures;
using Messages;

namespace TaskManager;

internal static class AnalysisService
{
    private const string GenerationResultFileName = "generation-result.json";
    private const string GraphDataFileName = "graph-data.json";
    private const string SettingsFileName = "analysis-settings.json";
    private const double ColorScaleMinValue = -0.1;
    private const double ColorScaleMaxValue = 0.1;

    internal static async Task<GenerateAnalysisGraphResponse> GenerateGraphAsync(
        IFileSystem fileSystem, string? projectFolder, string analysisName, TwoVariableAnalysisSettings settings)
    {
        if (string.IsNullOrWhiteSpace(projectFolder))
        {
            return PersistAndReturn(fileSystem, projectFolder, analysisName, settings, "No project was specified.", 0, 0, 0, 0);
        }

        WriteSettings(fileSystem, projectFolder, analysisName, settings);

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
        var linkedDatasetNames = await ReadLinkedDatasetNamesAsync(fileSystem, cohortFolder);

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

            if (!parsedSeries.Value.Channels.ContainsKey(settings.ChannelAGrid.ChannelName) ||
                !parsedSeries.Value.Channels.ContainsKey(settings.ChannelBGrid.ChannelName))
            {
                continue;
            }

            candidateSeries.Add(parsedSeries.Value);
            candidateOutcomes.Add(outcomeValue);
        }

        if (candidateSeries.Count == 0)
        {
            return PersistAndReturn(
                fileSystem, projectFolder, analysisName, settings,
                "No valid data was found for the configured channels among the cohort's matched patients.",
                totalPatientCount, 0, unmatchedCount, ambiguousCount);
        }

        var channelABinEdges = GridBinning.ComputeBinEdges(
            settings.ChannelAGrid.LowerLimit, settings.ChannelAGrid.UpperLimit, settings.ChannelAGrid.BinSize);
        var channelBBinEdges = GridBinning.ComputeBinEdges(
            settings.ChannelBGrid.LowerLimit, settings.ChannelBGrid.UpperLimit, settings.ChannelBGrid.BinSize);
        var rowCount = channelBBinEdges.Length - 1;
        var colCount = channelABinEdges.Length - 1;

        var matchedMatrices = new List<double[,]>();
        var matchedOutcomes = new List<double>();

        for (var i = 0; i < candidateSeries.Count; i++)
        {
            var series = candidateSeries[i];
            var percentMatrix = PercentTimeGrid.ComputePercentTimeMatrix(
                series.Channels[settings.ChannelAGrid.ChannelName],
                series.Channels[settings.ChannelBGrid.ChannelName],
                channelABinEdges,
                channelBBinEdges,
                TimeSeries.MissingValue,
                settings.ChannelAGrid.IsLeftInclusive,
                settings.ChannelBGrid.IsLeftInclusive);

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

        GraphDrawData graphDrawData;
        switch (GraphDrawData.Create(
            settings.ChannelAGrid.ChannelName, settings.ChannelBGrid.ChannelName,
            channelABinEdges.ToImmutableArray(), channelBBinEdges.ToImmutableArray(), cellColors.ToImmutableArray(),
            // Standalone-graph default: full chrome. Preserves today's AnalysisGraphPdfService behavior; a
            // GraphDrawData embedded in a Figure cell is drawn with these same flags, no longer hardcoded off.
            drawAxisTickLabels: true, drawAxisTitles: true))
        {
            case Success<GraphDrawData> success:
                graphDrawData = success.Value;
                break;
            case Failure<GraphDrawData> failure:
                return PersistAndReturn(
                    fileSystem, projectFolder, analysisName, settings, failure.Error,
                    totalPatientCount, matchedMatrices.Count, unmatchedCount, ambiguousCount);
            default:
                return PersistAndReturn(
                    fileSystem, projectFolder, analysisName, settings, "Could not build the graph data.",
                    totalPatientCount, matchedMatrices.Count, unmatchedCount, ambiguousCount);
        }

        var pdfOutputPath = AnalysisGraphPdfService.GeneratePdf(fileSystem, projectFolder, analysisName, graphDrawData);

        var response = new GenerateAnalysisGraphResponse(
            projectFolder,
            analysisName,
            Success: true,
            ErrorMessage: null,
            settings.CohortName,
            totalPatientCount,
            matchedMatrices.Count,
            unmatchedCount,
            ambiguousCount,
            graphDrawData,
            pdfOutputPath);

        WriteSummary(fileSystem, projectFolder, analysisName, settings, response);
        WriteGraphData(fileSystem, projectFolder, analysisName, graphDrawData);
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
            totalPatientCount,
            matchedPatientCount,
            unmatchedPatientCount,
            ambiguousPatientCount,
            Graph: null,
            PdfOutputPath: null);

        if (!string.IsNullOrWhiteSpace(projectFolder))
        {
            WriteSummary(fileSystem, projectFolder, analysisName, settings, response);
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
        IFileSystem fileSystem, string projectFolder, string analysisName, TwoVariableAnalysisSettings settings,
        GenerateAnalysisGraphResponse response)
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
                ChannelAName = settings.ChannelAGrid.ChannelName,
                ChannelBName = settings.ChannelBGrid.ChannelName,
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
        IFileSystem fileSystem, string projectFolder, string analysisName, GraphDrawData graph)
    {
        try
        {
            var graphDataFilePath = ResolveGraphDataFilePath(projectFolder, analysisName);
            fileSystem.CreateDirectory(Path.GetDirectoryName(graphDataFilePath)!);
            fileSystem.WriteAllBytes(graphDataFilePath, graph.ToByteArray().ToArray());
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // The response already carries the original outcome; a failure to persist it is not itself fatal.
        }
    }

    internal static GraphDrawData? ReadPersistedGraphData(
        IFileSystem fileSystem, string? projectFolder, string analysisName)
    {
        if (string.IsNullOrWhiteSpace(projectFolder))
        {
            return null;
        }

        var graphDataFilePath = ResolveGraphDataFilePath(projectFolder, analysisName);
        if (!fileSystem.FileExists(graphDataFilePath))
        {
            return null;
        }

        return GraphDrawData.FromByteArray(fileSystem.ReadAllBytes(graphDataFilePath).ToList()) is Success<GraphDrawData> success
            ? success.Value
            : null;
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
            .Where(name => ReadPersistedGraphData(fileSystem, projectFolder, name) is not null)
            .ToImmutableArray();
    }

    private static string ResolveGraphDataFilePath(string projectFolder, string analysisName)
    {
        return Path.Combine(projectFolder, "OutMapper_InternalFiles", "Analyses", analysisName, GraphDataFileName);
    }

    private static async Task<string[]> ReadLinkedDatasetNamesAsync(IFileSystem fileSystem, string cohortFolder)
    {
        var linkedDatasetsFile = Path.Combine(cohortFolder, "linked-datasets.json");
        if (!fileSystem.FileExists(linkedDatasetsFile))
        {
            return Array.Empty<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(await fileSystem.ReadAllBytesAsync(linkedDatasetsFile))
                ?? Array.Empty<string>();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    private static void WriteSettings(
        IFileSystem fileSystem, string projectFolder, string analysisName, TwoVariableAnalysisSettings settings)
    {
        try
        {
            var settingsFilePath = ResolveSettingsFilePath(projectFolder, analysisName);
            fileSystem.CreateDirectory(Path.GetDirectoryName(settingsFilePath)!);
            fileSystem.WriteAllBytes(settingsFilePath, settings.ToByteArray().ToArray());
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Persisting the user's settings is best-effort; a failure here must not block generation.
        }
    }

    internal static AnalysisSettingsResponse ReadPersistedSettings(
        IFileSystem fileSystem, string? projectFolder, string analysisName)
    {
        if (string.IsNullOrWhiteSpace(projectFolder))
        {
            return new AnalysisSettingsResponse(projectFolder ?? string.Empty, analysisName, Found: false, Settings: null);
        }

        var settingsFilePath = ResolveSettingsFilePath(projectFolder, analysisName);
        if (!fileSystem.FileExists(settingsFilePath))
        {
            return new AnalysisSettingsResponse(projectFolder, analysisName, Found: false, Settings: null);
        }

        if (TwoVariableAnalysisSettings.FromByteArray(fileSystem.ReadAllBytes(settingsFilePath).ToList())
            is not Success<TwoVariableAnalysisSettings> success)
        {
            return new AnalysisSettingsResponse(projectFolder, analysisName, Found: false, Settings: null);
        }

        return new AnalysisSettingsResponse(projectFolder, analysisName, Found: true, success.Value);
    }

    private static string ResolveSettingsFilePath(string projectFolder, string analysisName)
    {
        return Path.Combine(projectFolder, "OutMapper_InternalFiles", "Analyses", analysisName, SettingsFileName);
    }

    internal static async Task<ImmutableArray<string>> DiscoverChannelNamesAsync(
        IFileSystem fileSystem, string? projectFolder, string cohortName)
    {
        if (string.IsNullOrWhiteSpace(projectFolder) || string.IsNullOrWhiteSpace(cohortName))
        {
            return ImmutableArray<string>.Empty;
        }

        var cohortFolder = Path.Combine(projectFolder, "OutMapper_InternalFiles", "Cohorts", cohortName);
        var linkedDatasetNames = await ReadLinkedDatasetNamesAsync(fileSystem, cohortFolder);
        if (linkedDatasetNames.Length == 0)
        {
            return ImmutableArray<string>.Empty;
        }

        var datasetsFolder = Path.Combine(projectFolder, "OutMapper_InternalFiles", "Datasets");
        var channelNames = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var datasetName in linkedDatasetNames)
        {
            var parsedDataFolder = Path.Combine(datasetsFolder, datasetName, "Parsed data");
            if (!fileSystem.DirectoryExists(parsedDataFolder))
            {
                continue;
            }

            // Only the first patient file per dataset is scanned: channel sets are expected to be
            // uniform within a dataset, and scanning every patient file would be needlessly slow.
            var patientFile = fileSystem.GetFiles(parsedDataFolder, "*.json").FirstOrDefault();
            if (patientFile is null)
            {
                continue;
            }

            byte[] timeSeriesBytes;
            try
            {
                timeSeriesBytes = await fileSystem.ReadAllBytesAsync(patientFile);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            if (TimeSeries.FromByteArray(timeSeriesBytes.ToList()) is Success<TimeSeries> parsedSeries)
            {
                foreach (var channelName in parsedSeries.Value.Channels.Keys)
                {
                    channelNames.Add(channelName);
                }
            }
        }

        return channelNames.ToImmutableArray();
    }
}
