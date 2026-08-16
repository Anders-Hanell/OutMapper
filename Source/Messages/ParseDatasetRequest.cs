using DataStructures;

namespace Messages;

public sealed record ParseDatasetRequest(string ProjectFolder, string DatasetName, CsvParseParams ParseParams) : Message;
