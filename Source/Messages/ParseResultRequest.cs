namespace Messages;

public sealed record ParseResultRequest(string ProjectName, string DatasetName) : Message;
