namespace Messages;

public sealed record CreateDatasetRequest(string DatasetName, string ProjectName, string? RawDataFolderPath) : Message;
