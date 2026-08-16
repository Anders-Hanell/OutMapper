namespace Messages;

public sealed record CsvFileParseOutcome(string FileName, bool Success, string? ErrorMessage);
