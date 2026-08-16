namespace Messages;

public sealed record CreateCohortResponse(string CohortName, string? ProjectFolder, bool Success) : Message;
