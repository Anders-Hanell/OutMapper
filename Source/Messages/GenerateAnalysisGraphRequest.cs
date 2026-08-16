using DataStructures;

namespace Messages;

public sealed record GenerateAnalysisGraphRequest(string ProjectName, string AnalysisName, TwoVariableAnalysisSettings Settings) : Message;
