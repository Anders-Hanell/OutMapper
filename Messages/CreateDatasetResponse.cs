namespace Messages;

public sealed record CreateDatasetResponse(string DatasetName, bool Success) : Message;
