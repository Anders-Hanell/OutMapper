namespace Algorithms;

public static class GridBinning
{
    /// <summary>
    /// Builds bin edges starting at <paramref name="min"/>, stepping by <paramref name="binSize"/>,
    /// extended with enough bins to cover <paramref name="max"/>. Always returns at least one bin.
    /// </summary>
    public static double[] ComputeBinEdges(double min, double max, double binSize)
    {
        var span = Math.Max(0.0, max - min);
        var binCount = Math.Max(1, (int)Math.Ceiling(span / binSize));

        var edges = new double[binCount + 1];
        for (var i = 0; i <= binCount; i++)
        {
            edges[i] = min + i * binSize;
        }

        return edges;
    }

    /// <summary>
    /// Finds the index of the bin containing <paramref name="value"/>. Bins are half-open
    /// (lower inclusive, upper exclusive), except the last bin, which is closed on both ends.
    /// Returns null if the value falls outside the edges' range.
    /// </summary>
    public static int? FindBinIndex(double[] edges, double value)
    {
        var lastBinIndex = edges.Length - 2;
        for (var i = 0; i <= lastBinIndex; i++)
        {
            var isLastBin = i == lastBinIndex;
            if (value >= edges[i] && (isLastBin ? value <= edges[i + 1] : value < edges[i + 1]))
            {
                return i;
            }
        }

        return null;
    }
}
