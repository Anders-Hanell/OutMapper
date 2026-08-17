namespace Messages;

public sealed record AnalysisSettingsRequest(string ProjectFolder, string AnalysisName) : Message;
