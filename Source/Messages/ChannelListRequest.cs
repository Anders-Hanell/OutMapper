namespace Messages;

public sealed record ChannelListRequest(string ProjectFolder, string CohortName) : Message;
