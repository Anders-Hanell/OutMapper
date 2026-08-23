namespace Messages;

using DataStructures;

public sealed record ComputeHeatmapLayoutResponse(Guid RequestId, Result<HeatmapLayoutData> Result)
    : Message, ICorrelatedMessage;
