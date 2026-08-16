namespace Messages;

public sealed record CreateDatasetResponse(string DatasetName, string? ProjectName, bool Success) : Message;
