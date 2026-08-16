using System.Collections.Immutable;

namespace Messages;

public sealed record AnalysesWithGraphListResponse(string? ProjectName, ImmutableArray<string> AnalysisNames) : Message;
