using System.Collections.Immutable;

namespace Messages;

public sealed record CreateFigureGraphResponse(
    string ProjectName,
    string FigureName,
    bool Success,
    string? ErrorMessage,
    int RowCount,
    int ColCount,
    ImmutableArray<FigureCellGraphData> Cells) : Message;
