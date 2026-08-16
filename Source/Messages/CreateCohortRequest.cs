using System.Collections.Immutable;

namespace Messages;

public sealed record CreateCohortRequest(
    string CohortName, string ProjectName, string? RawCsvFilePath, ImmutableArray<string> LinkedDatasetNames) : Message;
