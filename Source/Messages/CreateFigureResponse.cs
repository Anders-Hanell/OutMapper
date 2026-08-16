namespace Messages;

public sealed record CreateFigureResponse(string FigureName, string? ProjectFolder, bool Success) : Message;
