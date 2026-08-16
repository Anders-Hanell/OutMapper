namespace Messages;

public sealed record CreateCohortResponse(string CohortName, string? ProjectName, bool Success) : Message;
