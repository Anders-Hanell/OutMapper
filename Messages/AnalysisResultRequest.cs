namespace Messages;

public sealed record AnalysisResultRequest(string ProjectName, string AnalysisName) : Message;
