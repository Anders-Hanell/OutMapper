namespace Messages;

using System.Collections.Immutable;
using DataStructures;

/// <summary>
/// Asks TaskManager to read each assigned cell's persisted graph data and assemble a Figure's draw
/// data, without persisting anything — used for OutMapper's live preview, which must not have the side
/// effect of saving the figure before the user clicks "Create". Correlated by <see cref="RequestId"/>
/// rather than routed to a screen.
/// </summary>
public sealed record BuildFigureDrawDataRequest(
    Guid RequestId,
    string ProjectFolder,
    int RowCount,
    int ColCount,
    ImmutableArray<string?> CellAnalysisNames,
    FigureLabelStyle LabelStyle) : Message, ICorrelatedMessage;
