using System.Collections.Immutable;

namespace Messages;

public sealed record ChannelListResponse(string ProjectFolder, string CohortName, ImmutableArray<string> ChannelNames) : Message;
