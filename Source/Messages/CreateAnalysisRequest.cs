namespace Messages;

public sealed record CreateAnalysisRequest(string AnalysisName, string ProjectName) : Message;
