using System.Collections.Immutable;

namespace Messages;

public sealed record DatasetListResponse(ImmutableArray<string> DatasetNames) : Message;
