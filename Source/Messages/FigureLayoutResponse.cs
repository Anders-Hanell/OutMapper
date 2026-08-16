using System.Collections.Immutable;

namespace Messages;

public sealed record FigureLayoutResponse(
    string? ProjectFolder,
    string? FigureName,
    bool LayoutExists,
    int RowCount,
    int ColCount,
    ImmutableArray<string?> CellAnalysisNames) : Message;
