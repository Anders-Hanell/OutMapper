using System.Collections.Immutable;

namespace Messages;

public sealed record FigureListResponse(string? ProjectName, ImmutableArray<string> FigureNames) : Message;
