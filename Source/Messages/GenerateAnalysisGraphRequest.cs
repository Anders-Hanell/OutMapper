using DataStructures;

namespace Messages;

public sealed record GenerateAnalysisGraphRequest(string ProjectFolder, string AnalysisName, TwoVariableAnalysisSettings Settings) : Message;
