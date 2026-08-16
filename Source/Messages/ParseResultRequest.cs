namespace Messages;

public sealed record ParseResultRequest(string ProjectFolder, string DatasetName) : Message;
