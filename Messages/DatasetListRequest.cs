namespace Messages;

public sealed record DatasetListRequest(string? ProjectName, string? WorkspaceFolder) : Message;
