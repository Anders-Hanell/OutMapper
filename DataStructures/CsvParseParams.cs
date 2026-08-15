using System.Globalization;

namespace DataStructures;

public sealed class CsvParseParams
{
    private static readonly DateTime FormatCanary = new(2001, 2, 3, 4, 5, 6);

    private CsvParseParams(char delimiterChar, char decimalSeparatorChar, string timeColumnHeader, string timestampFormatString)
    {
        DelimiterChar = delimiterChar;
        DecimalSeparatorChar = decimalSeparatorChar;
        TimeColumnHeader = timeColumnHeader;
        TimestampFormatString = timestampFormatString;
    }

    public char DelimiterChar { get; }
    public char DecimalSeparatorChar { get; }
    public string TimeColumnHeader { get; }
    public string TimestampFormatString { get; }

    public static Result<CsvParseParams> Create(
        char delimiterChar, char decimalSeparatorChar, string timeColumnHeader, string timestampFormatString)
    {
        if (decimalSeparatorChar != '.' && decimalSeparatorChar != ',')
        {
            return new Failure<CsvParseParams>("Decimal separator must be '.' or ','.");
        }

        if (delimiterChar == decimalSeparatorChar)
        {
            return new Failure<CsvParseParams>("Delimiter character must not be the same as the decimal separator character.");
        }

        if (delimiterChar is '"' or '\r' or '\n')
        {
            return new Failure<CsvParseParams>("Delimiter character must not be a quote character or a line ending.");
        }

        if (string.IsNullOrWhiteSpace(timeColumnHeader))
        {
            return new Failure<CsvParseParams>("Time column header must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(timestampFormatString))
        {
            return new Failure<CsvParseParams>("Timestamp format string must not be empty.");
        }

        try
        {
            // Only checks that formatting and re-parsing succeed, not that the result equals the canary:
            // a legitimate format (e.g. one that omits seconds) will lose precision on round-trip by design.
            var formatted = FormatCanary.ToString(timestampFormatString, CultureInfo.InvariantCulture);
            DateTime.ParseExact(formatted, timestampFormatString, CultureInfo.InvariantCulture, DateTimeStyles.None);
        }
        catch (FormatException)
        {
            return new Failure<CsvParseParams>($"Timestamp format string '{timestampFormatString}' is not a valid .NET date/time format.");
        }

        return new Success<CsvParseParams>(new CsvParseParams(delimiterChar, decimalSeparatorChar, timeColumnHeader, timestampFormatString));
    }
}
