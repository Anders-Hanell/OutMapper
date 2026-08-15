namespace Messages;

public sealed record CohortParseResultRequest(string ProjectName, string CohortName) : Message;
