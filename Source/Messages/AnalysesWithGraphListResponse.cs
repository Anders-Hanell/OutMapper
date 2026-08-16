using System.Collections.Immutable;

namespace Messages;

public sealed record AnalysesWithGraphListResponse(string? ProjectFolder, ImmutableArray<string> AnalysisNames) : Message;
