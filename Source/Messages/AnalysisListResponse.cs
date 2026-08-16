using System.Collections.Immutable;

namespace Messages;

public sealed record AnalysisListResponse(string? ProjectName, ImmutableArray<string> AnalysisNames) : Message;
