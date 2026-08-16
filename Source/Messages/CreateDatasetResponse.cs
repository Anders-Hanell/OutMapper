namespace Messages;

public sealed record CreateDatasetResponse(string DatasetName, string? ProjectFolder, bool Success) : Message;
