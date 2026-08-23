namespace Messages;

using DataStructures;

public sealed record BuildFigureDrawDataResponse(Guid RequestId, Result<FigureDrawData> Result)
    : Message, ICorrelatedMessage;
