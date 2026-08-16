namespace Messages;

public sealed record CohortParseResultResponse(
    string ProjectFolder,
    string CohortName,
    bool ParseHasRun,
    DateTime? ParsedAtUtc,
    bool Success,
    string? ErrorMessage,
    int PatientCount) : Message;
