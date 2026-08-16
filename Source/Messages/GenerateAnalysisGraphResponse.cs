using System.Collections.Immutable;

namespace Messages;

public sealed record GenerateAnalysisGraphResponse(
    string ProjectName,
    string AnalysisName,
    bool Success,
    string? ErrorMessage,
    string CohortName,
    string ChannelAName,
    string ChannelBName,
    int TotalPatientCount,
    int MatchedPatientCount,
    int UnmatchedPatientCount,
    int AmbiguousPatientCount,
    ImmutableArray<double> ChannelABinEdges,
    ImmutableArray<double> ChannelBBinEdges,
    ImmutableArray<string> CellColorsRowMajor) : Message;
