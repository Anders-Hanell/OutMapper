namespace Messages;

public sealed record CohortParseResultRequest(string ProjectFolder, string CohortName) : Message;
