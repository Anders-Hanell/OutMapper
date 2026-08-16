namespace Messages;

public sealed record FigureLayoutRequest(string ProjectFolder, string FigureName) : Message;
