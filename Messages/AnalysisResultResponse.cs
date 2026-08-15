namespace Messages;

public sealed record AnalysisResultResponse(
    string ProjectName,
    string AnalysisName,
    bool GenerationHasRun,
    DateTime? GeneratedAtUtc,
    bool Success,
    string? ErrorMessage,
    string? CohortName,
    string? ChannelAName,
    string? ChannelBName,
    int MatchedPatientCount,
    int TotalPatientCount) : Message;
