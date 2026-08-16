using System.Collections.Immutable;

namespace Messages;

public sealed record CohortListResponse(string? ProjectFolder, ImmutableArray<string> CohortNames) : Message;
