namespace Messages;

public sealed record AnalysisResultRequest(string ProjectFolder, string AnalysisName) : Message;
