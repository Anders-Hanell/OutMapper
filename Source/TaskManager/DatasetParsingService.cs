using System.Collections.Immutable;
using System.IO;
using System.Text.Json;

using Algorithms;
using DataStructures;
using Messages;

namespace TaskManager;

internal static class DatasetParsingService
{
    private const string ImportedRawDataFolderName = "Imported raw data";
    private const string ParsedDataFolderName = "Parsed data";
    private const string ParseResultFileName = "parse-result.json";

    internal static async Task<ParseResultResponse> ParseDatasetAsync(
        IFileSystem fileSystem, string? projectFolder, string datasetName, CsvParseParams parseParams)
    {
        if (string.IsNullOrWhiteSpace(projectFolder) || string.IsNullOrWhiteSpace(datasetName))
        {
            return PersistAndReturn(fileSystem, projectFolder, datasetName, "No project or dataset was specified.");
        }

        var (importedRawDataFolder, parsedDataFolder, summaryFilePath) =
            ResolveDatasetPaths(projectFolder, datasetName);

        if (!fileSystem.DirectoryExists(importedRawDataFolder))
        {
            return PersistAndReturn(fileSystem, projectFolder, datasetName, "The dataset's raw data folder was not found.");
        }

        var csvFiles = fileSystem.GetFiles(importedRawDataFolder, "*.csv");
        if (csvFiles.Length == 0)
        {
            return PersistAndReturn(fileSystem, projectFolder, datasetName, "No CSV files were found in 'Imported raw data'.");
        }

        try
        {
            fileSystem.CreateDirectory(parsedDataFolder);

            var outcomes = new List<CsvFileParseOutcome>(csvFiles.Length);

            foreach (var filePath in csvFiles)
            {
                var fileName = Path.GetFileName(filePath);
                outcomes.Add(await ParseSingleFileAsync(fileSystem, filePath, fileName, parsedDataFolder, parseParams));
            }

            var successCount = outcomes.Count(outcome => outcome.Success);
            var response = new ParseResultResponse(
                projectFolder,
                datasetName,
                ParseHasRun: true,
                ParsedAtUtc: DateTime.UtcNow,
                OverallError: null,
                TotalFileCount: outcomes.Count,
                SuccessCount: successCount,
                FailureCount: outcomes.Count - successCount,
                FileOutcomes: outcomes.ToImmutableArray());

            WriteSummary(fileSystem, summaryFilePath, response);
            return response;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return PersistAndReturn(fileSystem, projectFolder, datasetName, $"Could not parse dataset: {exception.Message}");
        }
    }

    internal static ParseResultResponse ReadPersistedParseResult(
        IFileSystem fileSystem, string? projectFolder, string datasetName)
    {
        if (string.IsNullOrWhiteSpace(projectFolder) || string.IsNullOrWhiteSpace(datasetName))
        {
            return NoParseHasRun(projectFolder ?? string.Empty, datasetName);
        }

        var (_, _, summaryFilePath) = ResolveDatasetPaths(projectFolder, datasetName);
        if (!fileSystem.FileExists(summaryFilePath))
        {
            return NoParseHasRun(projectFolder, datasetName);
        }

        try
        {
            var dto = JsonSerializer.Deserialize<ParseResultSummaryDto>(fileSystem.ReadAllBytes(summaryFilePath));
            if (dto is null)
            {
                return NoParseHasRun(projectFolder, datasetName);
            }

            return new ParseResultResponse(
                projectFolder,
                dto.DatasetName,
                ParseHasRun: true,
                ParsedAtUtc: dto.ParsedAtUtc,
                OverallError: dto.OverallError,
                TotalFileCount: dto.TotalFileCount,
                SuccessCount: dto.SuccessCount,
                FailureCount: dto.TotalFileCount - dto.SuccessCount,
                FileOutcomes: dto.FileOutcomes
                    .Select(outcome => new CsvFileParseOutcome(outcome.FileName, outcome.Success, outcome.ErrorMessage))
                    .ToImmutableArray());
        }
        catch (JsonException)
        {
            return NoParseHasRun(projectFolder, datasetName);
        }
    }

    private static async Task<CsvFileParseOutcome> ParseSingleFileAsync(
        IFileSystem fileSystem, string filePath, string fileName, string parsedDataFolder, CsvParseParams parseParams)
    {
        try
        {
            var bytes = (await fileSystem.ReadAllBytesAsync(filePath)).ToList();

            switch (Csv.ParseBytes(bytes, parseParams))
            {
                case Success<TimeSeries> success:
                    var outputPath = Path.Combine(parsedDataFolder, Path.GetFileNameWithoutExtension(fileName) + ".json");
                    await fileSystem.WriteAllBytesAsync(outputPath, success.Value.ToByteArray().ToArray());
                    return new CsvFileParseOutcome(fileName, Success: true, ErrorMessage: null);

                case Failure<TimeSeries> failure:
                    return new CsvFileParseOutcome(fileName, Success: false, failure.Error);

                default:
                    return new CsvFileParseOutcome(fileName, Success: false, "Unknown parse result.");
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new CsvFileParseOutcome(fileName, Success: false, $"Could not read file: {exception.Message}");
        }
    }

    private static ParseResultResponse PersistAndReturn(
        IFileSystem fileSystem, string? projectFolder, string? datasetName, string overallError)
    {
        var response = new ParseResultResponse(
            projectFolder ?? string.Empty,
            datasetName ?? string.Empty,
            ParseHasRun: true,
            ParsedAtUtc: DateTime.UtcNow,
            OverallError: overallError,
            TotalFileCount: 0,
            SuccessCount: 0,
            FailureCount: 0,
            FileOutcomes: ImmutableArray<CsvFileParseOutcome>.Empty);

        if (!string.IsNullOrWhiteSpace(projectFolder) && !string.IsNullOrWhiteSpace(datasetName))
        {
            try
            {
                var (_, _, summaryFilePath) = ResolveDatasetPaths(projectFolder, datasetName);
                WriteSummary(fileSystem, summaryFilePath, response);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // The response already carries the original error; a failure to persist it is not itself fatal.
            }
        }

        return response;
    }

    private static ParseResultResponse NoParseHasRun(string projectFolder, string datasetName)
    {
        return new ParseResultResponse(
            projectFolder,
            datasetName,
            ParseHasRun: false,
            ParsedAtUtc: null,
            OverallError: null,
            TotalFileCount: 0,
            SuccessCount: 0,
            FailureCount: 0,
            FileOutcomes: ImmutableArray<CsvFileParseOutcome>.Empty);
    }

    private static void WriteSummary(IFileSystem fileSystem, string summaryFilePath, ParseResultResponse response)
    {
        var dto = new ParseResultSummaryDto
        {
            DatasetName = response.DatasetName,
            ParsedAtUtc = response.ParsedAtUtc ?? DateTime.UtcNow,
            OverallError = response.OverallError,
            TotalFileCount = response.TotalFileCount,
            SuccessCount = response.SuccessCount,
            FileOutcomes = response.FileOutcomes
                .Select(outcome => new CsvFileParseOutcomeDto
                {
                    FileName = outcome.FileName,
                    Success = outcome.Success,
                    ErrorMessage = outcome.ErrorMessage
                })
                .ToList()
        };

        fileSystem.CreateDirectory(Path.GetDirectoryName(summaryFilePath)!);
        fileSystem.WriteAllBytes(summaryFilePath, JsonSerializer.SerializeToUtf8Bytes(dto));
    }

    private static (string ImportedRawDataFolder, string ParsedDataFolder, string SummaryFilePath) ResolveDatasetPaths(
        string projectFolder, string datasetName)
    {
        var datasetFolder = Path.Combine(projectFolder, "OutMapper_InternalFiles", "Datasets", datasetName);

        return (
            Path.Combine(datasetFolder, ImportedRawDataFolderName),
            Path.Combine(datasetFolder, ParsedDataFolderName),
            Path.Combine(datasetFolder, ParseResultFileName));
    }

    private sealed class ParseResultSummaryDto
    {
        public string DatasetName { get; set; } = "";
        public DateTime ParsedAtUtc { get; set; }
        public string? OverallError { get; set; }
        public int TotalFileCount { get; set; }
        public int SuccessCount { get; set; }
        public List<CsvFileParseOutcomeDto> FileOutcomes { get; set; } = new();
    }

    private sealed class CsvFileParseOutcomeDto
    {
        public string FileName { get; set; } = "";
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
