namespace Messages;

using DataStructures;

public sealed record RenderFigurePreviewResponse(Guid RequestId, Result<byte[]> Result) : Message, ICorrelatedMessage;
