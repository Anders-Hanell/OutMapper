using System.Collections.Immutable;
using DataStructures;

namespace Messages;

public sealed record CreateFigureGraphRequest(
    string ProjectFolder,
    string FigureName,
    int RowCount,
    int ColCount,
    ImmutableArray<string?> CellAnalysisNames,
    FigureLabelStyle LabelStyle = FigureLabelStyle.None) : Message;
