namespace Messages;

public sealed record DatasetListRequest(string? WorkspaceFolder) : Message;
