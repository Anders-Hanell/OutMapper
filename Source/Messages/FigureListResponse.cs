using System.Collections.Immutable;

namespace Messages;

public sealed record FigureListResponse(string? ProjectFolder, ImmutableArray<string> FigureNames) : Message;
