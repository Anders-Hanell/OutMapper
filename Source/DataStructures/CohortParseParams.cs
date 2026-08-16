namespace DataStructures;

public sealed class CohortParseParams
{
    private CohortParseParams(char delimiterChar, string patientIdColumnHeader, string outcomeColumnHeader)
    {
        DelimiterChar = delimiterChar;
        PatientIdColumnHeader = patientIdColumnHeader;
        OutcomeColumnHeader = outcomeColumnHeader;
    }

    public char DelimiterChar { get; }
    public string PatientIdColumnHeader { get; }
    public string OutcomeColumnHeader { get; }

    public static Result<CohortParseParams> Create(char delimiterChar, string patientIdColumnHeader, string outcomeColumnHeader)
    {
        if (delimiterChar is '"' or '\r' or '\n')
        {
            return new Failure<CohortParseParams>("Delimiter character must not be a quote character or a line ending.");
        }

        if (string.IsNullOrWhiteSpace(patientIdColumnHeader))
        {
            return new Failure<CohortParseParams>("Patient ID column header must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(outcomeColumnHeader))
        {
            return new Failure<CohortParseParams>("Outcome column header must not be empty.");
        }

        if (patientIdColumnHeader == outcomeColumnHeader)
        {
            return new Failure<CohortParseParams>("Patient ID column header and outcome column header must be different.");
        }

        return new Success<CohortParseParams>(new CohortParseParams(delimiterChar, patientIdColumnHeader, outcomeColumnHeader));
    }
}
