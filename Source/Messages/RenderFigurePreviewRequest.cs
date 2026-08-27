namespace Messages;

using System.Collections.Immutable;
using DataStructures;

/// <summary>
/// Asks TaskManager to assemble a Figure's draw data from its currently assigned cells (without
/// persisting anything) and render it to a PNG for OutMapper's live preview.
/// </summary>
public sealed record RenderFigurePreviewRequest(
    string ProjectFolder,
    int RowCount,
    int ColCount,
    ImmutableArray<string?> CellAnalysisNames,
    FigureLabelStyle LabelStyle,
    int MaxDimensionPx) : Message;
