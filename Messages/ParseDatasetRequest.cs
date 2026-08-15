using DataStructures;

namespace Messages;

public sealed record ParseDatasetRequest(string ProjectName, string DatasetName, CsvParseParams ParseParams) : Message;
