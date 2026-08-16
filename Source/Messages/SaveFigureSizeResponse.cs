using System.Collections.Immutable;

namespace Messages;

public sealed record SaveFigureSizeResponse(
    string? ProjectName,
    string? FigureName,
    bool Success,
    string? ErrorMessage,
    int RowCount,
    int ColCount,
    ImmutableArray<string?> CellAnalysisNames) : Message;
