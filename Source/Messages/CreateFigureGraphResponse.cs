using DataStructures;

namespace Messages;

public sealed record CreateFigureGraphResponse(
    string ProjectFolder,
    string FigureName,
    bool Success,
    string? ErrorMessage,
    FigureDrawData? Figure) : Message;
