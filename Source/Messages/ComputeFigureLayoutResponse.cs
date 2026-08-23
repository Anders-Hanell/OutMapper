namespace Messages;

using DataStructures;

public sealed record ComputeFigureLayoutResponse(Guid RequestId, Result<FigureLayoutData> Result)
    : Message, ICorrelatedMessage;
