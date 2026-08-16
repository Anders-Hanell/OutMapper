using DataStructures;

namespace Messages;

public sealed record ParseCohortRequest(string ProjectFolder, string CohortName, CohortParseParams ParseParams) : Message;
