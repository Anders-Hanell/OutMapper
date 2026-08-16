namespace Messages;

public sealed record CreateFigureResponse(string FigureName, string? ProjectName, bool Success) : Message;
