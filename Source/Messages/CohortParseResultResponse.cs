namespace Messages;

public sealed record CohortParseResultResponse(
    string ProjectName,
    string CohortName,
    bool ParseHasRun,
    DateTime? ParsedAtUtc,
    bool Success,
    string? ErrorMessage,
    int PatientCount) : Message;
