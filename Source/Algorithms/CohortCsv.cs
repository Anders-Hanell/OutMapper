using System.Text;

using DataStructures;

namespace Algorithms;

public static class CohortCsv
{
    public static Result<Cohort> ParseBytes(List<byte> bytes, CohortParseParams parseParams)
    {
        var text = DecodeUtf8(bytes.ToArray());
        var lines = SplitIntoLines(text);

        if (lines.Count == 0)
        {
            return new Failure<Cohort>("The file is empty.");
        }

        var headers = lines[0].Split(parseParams.DelimiterChar);

        var duplicateHeader = headers
            .GroupBy(header => header)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateHeader is not null)
        {
            return new Failure<Cohort>($"Column header '{duplicateHeader.Key}' appears more than once.");
        }

        var patientIdColumnIndex = Array.IndexOf(headers, parseParams.PatientIdColumnHeader);
        if (patientIdColumnIndex < 0)
        {
            return new Failure<Cohort>($"Patient ID column header '{parseParams.PatientIdColumnHeader}' was not found.");
        }

        var outcomeColumnIndex = Array.IndexOf(headers, parseParams.OutcomeColumnHeader);
        if (outcomeColumnIndex < 0)
        {
            return new Failure<Cohort>($"Outcome column header '{parseParams.OutcomeColumnHeader}' was not found.");
        }

        var dataLines = lines.Skip(1).ToList();
        if (dataLines.Count == 0)
        {
            return new Failure<Cohort>("The file contains a header but no data rows.");
        }

        var patientIds = new List<string>(dataLines.Count);
        var outcomes = new List<string>(dataLines.Count);

        for (var rowIndex = 0; rowIndex < dataLines.Count; rowIndex++)
        {
            var rowNumber = rowIndex + 2; // Header is row 1, so the first data row is row 2.
            var fields = dataLines[rowIndex].Split(parseParams.DelimiterChar);

            if (fields.Length != headers.Length)
            {
                return new Failure<Cohort>(
                    $"Row {rowNumber}: expected {headers.Length} field(s) but found {fields.Length}.");
            }

            patientIds.Add(fields[patientIdColumnIndex]);
            outcomes.Add(fields[outcomeColumnIndex]);
        }

        return Cohort.Create(patientIds, outcomes);
    }

    private static string DecodeUtf8(byte[] bytes)
    {
        var hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        return hasBom
            ? Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3)
            : Encoding.UTF8.GetString(bytes);
    }

    private static List<string> SplitIntoLines(string text)
    {
        var lines = text.ReplaceLineEndings("\n").Split('\n').ToList();

        if (lines.Count > 0 && lines[^1].Length == 0)
        {
            lines.RemoveAt(lines.Count - 1);
        }

        return lines;
    }
}
