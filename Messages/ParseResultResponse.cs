using System.Collections.Immutable;

namespace Messages;

public sealed record ParseResultResponse(
    string ProjectName,
    string DatasetName,
    bool ParseHasRun,
    DateTime? ParsedAtUtc,
    string? OverallError,
    int TotalFileCount,
    int SuccessCount,
    int FailureCount,
    ImmutableArray<CsvFileParseOutcome> FileOutcomes) : Message;
