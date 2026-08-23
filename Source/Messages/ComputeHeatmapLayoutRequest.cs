namespace Messages;

using DataStructures;

/// <summary>
/// Asks TaskManager to compute a heatmap's cell/tick/title geometry for the given draw area. Distinct
/// from <see cref="FigureLayoutRequest"/>, which reads a Figure's saved grid configuration — this is a
/// pure geometry computation, correlated by <see cref="RequestId"/> rather than routed to a screen.
/// </summary>
public sealed record ComputeHeatmapLayoutRequest(
    Guid RequestId,
    GraphDrawData Data,
    double AreaLeft,
    double AreaTop,
    double AreaWidth,
    double AreaHeight) : Message, ICorrelatedMessage;
