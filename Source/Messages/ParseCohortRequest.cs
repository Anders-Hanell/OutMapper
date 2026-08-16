using DataStructures;

namespace Messages;

public sealed record ParseCohortRequest(string ProjectName, string CohortName, CohortParseParams ParseParams) : Message;
