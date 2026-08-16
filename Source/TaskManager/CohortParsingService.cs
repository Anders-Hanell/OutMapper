using System.IO;
using System.Text.Json;

using Algorithms;
using DataStructures;
using Messages;

namespace TaskManager;

internal static class CohortParsingService
{
    private const string ImportedRawDataFolderName = "Imported raw data";
    private const string ParsedDataFolderName = "Parsed data";
    private const string ParsedCohortFileName = "cohort.json";
    private const string ParseResultFileName = "parse-result.json";

    internal static async Task<CohortParseResultResponse> ParseCohortAsync(
        IFileSystem fileSystem, string? workspaceFolder, string projectName, string cohortName, CohortParseParams parseParams)
    {
        if (string.IsNullOrWhiteSpace(workspaceFolder) ||
            string.IsNullOrWhiteSpace(projectName) ||
            string.IsNullOrWhiteSpace(cohortName))
        {
            return PersistAndReturn(fileSystem, workspaceFolder, projectName, cohortName, "No workspace, project, or cohort was specified.");
        }

        var (importedRawDataFolder, parsedDataFolder, summaryFilePath) =
            ResolveCohortPaths(workspaceFolder, projectName, cohortName);

        if (!fileSystem.DirectoryExists(importedRawDataFolder))
        {
            return PersistAndReturn(fileSystem, workspaceFolder, projectName, cohortName, "The cohort's raw data folder was not found.");
        }

        var csvFiles = fileSystem.GetFiles(importedRawDataFolder, "*.csv");
        if (csvFiles.Length == 0)
        {
            return PersistAndReturn(fileSystem, workspaceFolder, projectName, cohortName, "No CSV file was found in 'Imported raw data'.");
        }

        if (csvFiles.Length > 1)
        {
            return PersistAndReturn(fileSystem, workspaceFolder, projectName, cohortName, "More than one CSV file was found in 'Imported raw data'; expected exactly one.");
        }

        try
        {
            fileSystem.CreateDirectory(parsedDataFolder);

            var bytes = (await fileSystem.ReadAllBytesAsync(csvFiles[0])).ToList();

            switch (CohortCsv.ParseBytes(bytes, parseParams))
            {
                case Success<Cohort> success:
                    var outputPath = Path.Combine(parsedDataFolder, ParsedCohortFileName);
                    await fileSystem.WriteAllBytesAsync(outputPath, success.Value.ToByteArray().ToArray());

                    var response = new CohortParseResultResponse(
                        projectName,
                        cohortName,
                        ParseHasRun: true,
                        ParsedAtUtc: DateTime.UtcNow,
                        Success: true,
                        ErrorMessage: null,
                        PatientCount: success.Value.PatientIds.Length);

                    WriteSummary(fileSystem, summaryFilePath, response);
                    return response;

                case Failure<Cohort> failure:
                    return PersistAndReturn(fileSystem, workspaceFolder, projectName, cohortName, failure.Error);

                default:
                    return PersistAndReturn(fileSystem, workspaceFolder, projectName, cohortName, "Unknown parse result.");
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return PersistAndReturn(fileSystem, workspaceFolder, projectName, cohortName, $"Could not parse cohort: {exception.Message}");
        }
    }

    internal static CohortParseResultResponse ReadPersistedParseResult(
        IFileSystem fileSystem, string? workspaceFolder, string projectName, string cohortName)
    {
        if (string.IsNullOrWhiteSpace(workspaceFolder) ||
            string.IsNullOrWhiteSpace(projectName) ||
            string.IsNullOrWhiteSpace(cohortName))
        {
            return NoParseHasRun(projectName, cohortName);
        }

        var (_, _, summaryFilePath) = ResolveCohortPaths(workspaceFolder, projectName, cohortName);
        if (!fileSystem.FileExists(summaryFilePath))
        {
            return NoParseHasRun(projectName, cohortName);
        }

        try
        {
            var dto = JsonSerializer.Deserialize<ParseResultSummaryDto>(fileSystem.ReadAllBytes(summaryFilePath));
            if (dto is null)
            {
                return NoParseHasRun(projectName, cohortName);
            }

            return new CohortParseResultResponse(
                dto.ProjectName,
                dto.CohortName,
                ParseHasRun: true,
                ParsedAtUtc: dto.ParsedAtUtc,
                Success: dto.Success,
                ErrorMessage: dto.ErrorMessage,
                PatientCount: dto.PatientCount);
        }
        catch (JsonException)
        {
            return NoParseHasRun(projectName, cohortName);
        }
    }

    private static CohortParseResultResponse PersistAndReturn(
        IFileSystem fileSystem, string? workspaceFolder, string? projectName, string? cohortName, string errorMessage)
    {
        var response = new CohortParseResultResponse(
            projectName ?? string.Empty,
            cohortName ?? string.Empty,
            ParseHasRun: true,
            ParsedAtUtc: DateTime.UtcNow,
            Success: false,
            ErrorMessage: errorMessage,
            PatientCount: 0);

        if (!string.IsNullOrWhiteSpace(workspaceFolder) && !string.IsNullOrWhiteSpace(projectName) && !string.IsNullOrWhiteSpace(cohortName))
        {
            try
            {
                var (_, _, summaryFilePath) = ResolveCohortPaths(workspaceFolder, projectName, cohortName);
                WriteSummary(fileSystem, summaryFilePath, response);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // The response already carries the original error; a failure to persist it is not itself fatal.
            }
        }

        return response;
    }

    private static CohortParseResultResponse NoParseHasRun(string projectName, string cohortName)
    {
        return new CohortParseResultResponse(
            projectName,
            cohortName,
            ParseHasRun: false,
            ParsedAtUtc: null,
            Success: false,
            ErrorMessage: null,
            PatientCount: 0);
    }

    private static void WriteSummary(IFileSystem fileSystem, string summaryFilePath, CohortParseResultResponse response)
    {
        var dto = new ParseResultSummaryDto
        {
            ProjectName = response.ProjectName,
            CohortName = response.CohortName,
            ParsedAtUtc = response.ParsedAtUtc ?? DateTime.UtcNow,
            Success = response.Success,
            ErrorMessage = response.ErrorMessage,
            PatientCount = response.PatientCount
        };

        fileSystem.CreateDirectory(Path.GetDirectoryName(summaryFilePath)!);
        fileSystem.WriteAllBytes(summaryFilePath, JsonSerializer.SerializeToUtf8Bytes(dto));
    }

    private static (string ImportedRawDataFolder, string ParsedDataFolder, string SummaryFilePath) ResolveCohortPaths(
        string workspaceFolder, string projectName, string cohortName)
    {
        var cohortFolder = Path.Combine(
            workspaceFolder, "Projects", projectName, "OutMapper_InternalFiles", "Cohorts", cohortName);

        return (
            Path.Combine(cohortFolder, ImportedRawDataFolderName),
            Path.Combine(cohortFolder, ParsedDataFolderName),
            Path.Combine(cohortFolder, ParseResultFileName));
    }

    private sealed class ParseResultSummaryDto
    {
        public string ProjectName { get; set; } = "";
        public string CohortName { get; set; } = "";
        public DateTime ParsedAtUtc { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int PatientCount { get; set; }
    }
}
