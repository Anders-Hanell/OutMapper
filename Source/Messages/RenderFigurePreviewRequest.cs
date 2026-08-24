namespace Messages;

using System.Collections.Immutable;
using DataStructures;

/// <summary>
/// Asks TaskManager to assemble a Figure's draw data from its currently assigned cells (without
/// persisting anything) and render it to a PNG for OutMapper's live preview. Correlated by
/// <see cref="RequestId"/> rather than routed to a screen, since it must not be confused with a slower
/// or faster in-flight preview request for the same screen.
/// </summary>
public sealed record RenderFigurePreviewRequest(
    Guid RequestId,
    string ProjectFolder,
    int RowCount,
    int ColCount,
    ImmutableArray<string?> CellAnalysisNames,
    FigureLabelStyle LabelStyle,
    int MaxDimensionPx) : Message, ICorrelatedMessage;
