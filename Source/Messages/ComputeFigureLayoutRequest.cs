namespace Messages;

using DataStructures;

/// <summary>
/// Asks TaskManager to compute a Figure's grid-of-heatmaps geometry. Distinct from
/// <see cref="FigureLayoutRequest"/>, which reads a Figure's saved grid configuration — this is a pure
/// geometry computation, correlated by <see cref="RequestId"/> rather than routed to a screen.
/// </summary>
public sealed record ComputeFigureLayoutRequest(Guid RequestId, FigureDrawData Figure) : Message, ICorrelatedMessage;
