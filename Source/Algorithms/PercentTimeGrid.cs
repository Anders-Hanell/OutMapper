namespace Algorithms;

public static class PercentTimeGrid
{
    /// <summary>
    /// Computes, for one patient, the percent of their valid joint monitoring time spent in each
    /// (channel B bin, channel A bin) grid cell. "Valid joint" means both channels are non-missing
    /// and fall within their channel's bin edges at that timestamp; a value outside its channel's
    /// range is treated the same as a missing value. Rows correspond to channel B bins, columns to
    /// channel A bins. Returns null if the patient has zero valid joint observations, so callers can
    /// exclude them rather than treating them as 0% everywhere.
    /// </summary>
    public static double[,]? ComputePercentTimeMatrix(
        IReadOnlyList<float> channelAValues,
        IReadOnlyList<float> channelBValues,
        double[] channelABinEdges,
        double[] channelBBinEdges,
        float missingValue,
        bool channelAIsLeftInclusive = true,
        bool channelBIsLeftInclusive = true)
    {
        var rowCount = channelBBinEdges.Length - 1;
        var colCount = channelABinEdges.Length - 1;
        var counts = new int[rowCount, colCount];
        var validCount = 0;

        var length = Math.Min(channelAValues.Count, channelBValues.Count);
        for (var i = 0; i < length; i++)
        {
            var valueA = channelAValues[i];
            var valueB = channelBValues[i];
            if (valueA == missingValue || valueB == missingValue)
            {
                continue;
            }

            var colIndex = GridBinning.FindBinIndex(channelABinEdges, valueA, channelAIsLeftInclusive);
            var rowIndex = GridBinning.FindBinIndex(channelBBinEdges, valueB, channelBIsLeftInclusive);
            if (colIndex is not int col || rowIndex is not int row)
            {
                // Outside the configured range for one of the channels; treated as missing.
                continue;
            }

            validCount++;
            counts[row, col]++;
        }

        if (validCount == 0)
        {
            return null;
        }

        var percentMatrix = new double[rowCount, colCount];
        for (var row = 0; row < rowCount; row++)
        {
            for (var col = 0; col < colCount; col++)
            {
                percentMatrix[row, col] = counts[row, col] / (double)validCount * 100.0;
            }
        }

        return percentMatrix;
    }
}
