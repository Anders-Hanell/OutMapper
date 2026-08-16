namespace Messages;

public sealed record FigureLayoutRequest(string ProjectName, string FigureName) : Message;
