namespace Messages;

public sealed record CreateDatasetRequest(string DatasetName, string? WorkspaceFolder) : Message;
