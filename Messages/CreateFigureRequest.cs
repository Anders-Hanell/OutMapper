namespace Messages;

public sealed record CreateFigureRequest(string FigureName, string ProjectName) : Message;
