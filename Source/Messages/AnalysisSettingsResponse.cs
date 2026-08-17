using DataStructures;

namespace Messages;

public sealed record AnalysisSettingsResponse(
    string ProjectFolder, string AnalysisName, bool Found, TwoVariableAnalysisSettings? Settings) : Message;
