namespace Messages;

public sealed record CreateAnalysisResponse(string AnalysisName, string? ProjectFolder, bool Success) : Message;
