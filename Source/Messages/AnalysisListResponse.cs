using System.Collections.Immutable;

namespace Messages;

public sealed record AnalysisListResponse(string? ProjectFolder, ImmutableArray<string> AnalysisNames) : Message;
