namespace Messages;

public sealed record CreateDatasetRequest(string DatasetName, string ProjectFolder, string? RawDataFolderPath) : Message;
