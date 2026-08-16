namespace Messages;

public sealed record CreateAnalysisRequest(string AnalysisName, string ProjectFolder) : Message;
