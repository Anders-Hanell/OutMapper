using System.Collections.Immutable;

namespace Messages;

public sealed record CreateFigureGraphRequest(
    string ProjectName,
    string FigureName,
    int RowCount,
    int ColCount,
    ImmutableArray<string?> CellAnalysisNames) : Message;
