using System.Collections.Immutable;

namespace Messages;

public sealed record DatasetListResponse(string? ProjectFolder, ImmutableArray<string> DatasetNames) : Message;
