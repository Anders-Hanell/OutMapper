using DataStructures;

namespace Messages;

public sealed record GenerateAnalysisGraphResponse(
    string ProjectFolder,
    string AnalysisName,
    bool Success,
    string? ErrorMessage,
    string CohortName,
    int TotalPatientCount,
    int MatchedPatientCount,
    int UnmatchedPatientCount,
    int AmbiguousPatientCount,
    GraphDrawData? Graph,
    string? PdfOutputPath) : Message;
