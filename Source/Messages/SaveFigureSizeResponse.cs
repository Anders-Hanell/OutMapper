using System.Collections.Immutable;
using DataStructures;

namespace Messages;

public sealed record SaveFigureSizeResponse(
    string? ProjectFolder,
    string? FigureName,
    bool Success,
    string? ErrorMessage,
    int RowCount,
    int ColCount,
    ImmutableArray<string?> CellAnalysisNames,
    FigureLabelStyle LabelStyle = FigureLabelStyle.None) : Message;
