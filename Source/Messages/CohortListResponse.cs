using System.Collections.Immutable;

namespace Messages;

public sealed record CohortListResponse(string? ProjectName, ImmutableArray<string> CohortNames) : Message;
