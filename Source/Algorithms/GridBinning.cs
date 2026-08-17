namespace Algorithms;

public static class GridBinning
{
    /// <summary>
    /// Builds bin edges starting at <paramref name="min"/>, stepping by <paramref name="binSize"/>,
    /// with enough bins to reach <paramref name="max"/>. The edges never extend past
    /// <paramref name="max"/>: if the span isn't a whole number of bins, the last bin is narrower
    /// than the others. Always returns at least one bin.
    /// </summary>
    public static double[] ComputeBinEdges(double min, double max, double binSize)
    {
        var span = Math.Max(0.0, max - min);
        var binCount = Math.Max(1, (int)Math.Ceiling(span / binSize));

        var edges = new double[binCount + 1];
        for (var i = 0; i < binCount; i++)
        {
            edges[i] = min + i * binSize;
        }

        edges[binCount] = max;

        return edges;
    }

    /// <summary>
    /// Finds the index of the bin containing <paramref name="value"/>. When <paramref name="isLeftInclusive"/>
    /// is true (the default), bins are half-open (lower inclusive, upper exclusive), except the last bin,
    /// which is closed on both ends. When false, bins are mirrored (lower exclusive, upper inclusive),
    /// except the first bin, which is closed on both ends. Returns null if the value falls outside the
    /// edges' range.
    /// </summary>
    public static int? FindBinIndex(double[] edges, double value, bool isLeftInclusive = true)
    {
        var lastBinIndex = edges.Length - 2;
        for (var i = 0; i <= lastBinIndex; i++)
        {
            if (isLeftInclusive)
            {
                var isLastBin = i == lastBinIndex;
                if (value >= edges[i] && (isLastBin ? value <= edges[i + 1] : value < edges[i + 1]))
                {
                    return i;
                }
            }
            else
            {
                var isFirstBin = i == 0;
                if (value <= edges[i + 1] && (isFirstBin ? value >= edges[i] : value > edges[i]))
                {
                    return i;
                }
            }
        }

        return null;
    }
}
