namespace Algorithms;

public static class PercentTimeGrid
{
    /// <summary>
    /// Computes, for one patient, the percent of their valid joint monitoring time spent in each
    /// (channel B bin, channel A bin) grid cell. "Valid joint" means both channels are non-missing
    /// at that timestamp. Rows correspond to channel B bins, columns to channel A bins. Returns null
    /// if the patient has zero valid joint observations, so callers can exclude them rather than
    /// treating them as 0% everywhere.
    /// </summary>
    public static double[,]? ComputePercentTimeMatrix(
        IReadOnlyList<float> channelAValues,
        IReadOnlyList<float> channelBValues,
        double[] channelABinEdges,
        double[] channelBBinEdges,
        float missingValue)
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

            validCount++;

            var colIndex = GridBinning.FindBinIndex(channelABinEdges, valueA);
            var rowIndex = GridBinning.FindBinIndex(channelBBinEdges, valueB);
            if (colIndex is int col && rowIndex is int row)
            {
                counts[row, col]++;
            }
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
