namespace Messages;

using DataStructures;

public sealed record RenderFigurePreviewResponse(Result<byte[]> Result) : Message;
