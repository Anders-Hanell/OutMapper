using System.Collections.Immutable;

namespace Messages;

public sealed record FigureCellGraphData(
    int Row,
    int Col,
    string? AnalysisName,
    bool HasGraph,
    string? ErrorMessage,
    string? ChannelAName,
    string? ChannelBName,
    ImmutableArray<double> ChannelABinEdges,
    ImmutableArray<double> ChannelBBinEdges,
    ImmutableArray<string> CellColorsRowMajor);
