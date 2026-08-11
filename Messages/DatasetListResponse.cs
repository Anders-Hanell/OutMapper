using System.Collections.Immutable;

namespace Messages;

public sealed record DatasetListResponse(string? ProjectName, ImmutableArray<string> DatasetNames) : Message;
