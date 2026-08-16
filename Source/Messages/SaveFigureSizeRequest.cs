namespace Messages;

public sealed record SaveFigureSizeRequest(string ProjectFolder, string FigureName, int RowCount, int ColCount) : Message;
