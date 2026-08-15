namespace Messages;

public sealed record CreateAnalysisResponse(string AnalysisName, string? ProjectName, bool Success) : Message;
