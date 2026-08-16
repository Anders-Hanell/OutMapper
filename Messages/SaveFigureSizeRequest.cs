namespace Messages;

public sealed record SaveFigureSizeRequest(string ProjectName, string FigureName, int RowCount, int ColCount) : Message;
