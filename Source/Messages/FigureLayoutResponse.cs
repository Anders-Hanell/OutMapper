using System.Collections.Immutable;
using DataStructures;

namespace Messages;

public sealed record FigureLayoutResponse(
    string? ProjectFolder,
    string? FigureName,
    bool LayoutExists,
    int RowCount,
    int ColCount,
    ImmutableArray<string?> CellAnalysisNames,
    FigureLabelStyle LabelStyle = FigureLabelStyle.None) : Message;
