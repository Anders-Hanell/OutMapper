using System.Collections.Immutable;

namespace Messages;

public sealed record FigureLayoutResponse(
    string? ProjectName,
    string? FigureName,
    bool LayoutExists,
    int RowCount,
    int ColCount,
    ImmutableArray<string?> CellAnalysisNames) : Message;
