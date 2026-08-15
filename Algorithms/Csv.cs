using System.Globalization;
using System.Text;

using DataStructures;

namespace Algorithms;

public static class Csv
{
    public static Result<TimeSeries> ParseBytes(List<byte> bytes, CsvParseParams parseParams)
    {
        var text = DecodeUtf8(bytes.ToArray());
        var lines = SplitIntoLines(text);

        if (lines.Count == 0)
        {
            return new Failure<TimeSeries>("The file is empty.");
        }

        var headers = lines[0].Split(parseParams.DelimiterChar);

        if (headers.Length < 2)
        {
            return new Failure<TimeSeries>(
                $"Only {headers.Length} column(s) were found after splitting on '{parseParams.DelimiterChar}'. " +
                "Check that the correct delimiter was selected.");
        }

        var duplicateHeader = headers
            .GroupBy(header => header)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateHeader is not null)
        {
            return new Failure<TimeSeries>($"Column header '{duplicateHeader.Key}' appears more than once.");
        }

        var timeColumnIndex = Array.IndexOf(headers, parseParams.TimeColumnHeader);
        if (timeColumnIndex < 0)
        {
            return new Failure<TimeSeries>($"Time column header '{parseParams.TimeColumnHeader}' was not found.");
        }

        var dataLines = lines.Skip(1).ToList();
        if (dataLines.Count == 0)
        {
            return new Failure<TimeSeries>("The file contains a header but no data rows.");
        }

        var channelNames = headers.Where((_, index) => index != timeColumnIndex).ToList();

        var timestamps = new List<DateTime>(dataLines.Count);
        var channelValues = channelNames.ToDictionary(
            channelName => channelName,
            _ => new List<float>(dataLines.Count));

        for (var rowIndex = 0; rowIndex < dataLines.Count; rowIndex++)
        {
            var rowNumber = rowIndex + 2; // Header is row 1, so the first data row is row 2.
            var fields = dataLines[rowIndex].Split(parseParams.DelimiterChar);

            if (fields.Length != headers.Length)
            {
                return new Failure<TimeSeries>(
                    $"Row {rowNumber}: expected {headers.Length} field(s) but found {fields.Length}.");
            }

            var timeCell = fields[timeColumnIndex];
            if (timeCell.Length == 0)
            {
                return new Failure<TimeSeries>($"Row {rowNumber}: timestamp value is missing.");
            }

            if (!DateTime.TryParseExact(
                    timeCell, parseParams.TimestampFormatString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp))
            {
                return new Failure<TimeSeries>(
                    $"Row {rowNumber}: could not parse '{timeCell}' as a timestamp using format '{parseParams.TimestampFormatString}'.");
            }

            if (timestamps.Count > 0 && timestamp <= timestamps[^1])
            {
                return new Failure<TimeSeries>(
                    $"Row {rowNumber}: timestamp '{timeCell}' is not strictly greater than the previous timestamp.");
            }

            timestamps.Add(timestamp);

            for (var headerIndex = 0; headerIndex < headers.Length; headerIndex++)
            {
                if (headerIndex == timeColumnIndex)
                {
                    continue;
                }

                var channelName = headers[headerIndex];
                var cell = fields[headerIndex];

                if (cell.Length == 0)
                {
                    channelValues[channelName].Add(TimeSeries.MissingValue);
                    continue;
                }

                var adjustedCell = parseParams.DecimalSeparatorChar == ','
                    ? ReplaceFirst(cell, ',', '.')
                    : cell;

                if (!float.TryParse(adjustedCell, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                    float.IsNaN(value) || float.IsInfinity(value))
                {
                    return new Failure<TimeSeries>(
                        $"Row {rowNumber}, column '{channelName}': could not parse '{cell}' as a number.");
                }

                channelValues[channelName].Add(value);
            }
        }

        var readOnlyChannelValues = channelValues.ToDictionary(
            pair => pair.Key,
            IReadOnlyList<float> (pair) => pair.Value);

        return TimeSeries.Create(timestamps, readOnlyChannelValues);
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

    private static string ReplaceFirst(string input, char oldChar, char newChar)
    {
        var index = input.IndexOf(oldChar);
        return index < 0 ? input : input[..index] + newChar + input[(index + 1)..];
    }
}
