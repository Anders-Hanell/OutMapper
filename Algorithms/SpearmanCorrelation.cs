namespace Algorithms;

public static class SpearmanCorrelation
{
    /// <summary>
    /// Computes Spearman's rank correlation coefficient: the Pearson correlation of the two
    /// vectors' ranks, using average ranks for ties. Returns null if there are fewer than 2
    /// observations, or if either vector has zero variance (all values tied), since the
    /// correlation is undefined in that case.
    /// </summary>
    public static double? Compute(IReadOnlyList<double> x, IReadOnlyList<double> y)
    {
        var n = Math.Min(x.Count, y.Count);
        if (n < 2)
        {
            return null;
        }

        var rankX = Rank(x);
        var rankY = Rank(y);

        var meanX = rankX.Average();
        var meanY = rankY.Average();

        var numerator = 0.0;
        var sumSquaresX = 0.0;
        var sumSquaresY = 0.0;
        for (var i = 0; i < n; i++)
        {
            var deviationX = rankX[i] - meanX;
            var deviationY = rankY[i] - meanY;
            numerator += deviationX * deviationY;
            sumSquaresX += deviationX * deviationX;
            sumSquaresY += deviationY * deviationY;
        }

        if (sumSquaresX == 0 || sumSquaresY == 0)
        {
            return null;
        }

        return numerator / Math.Sqrt(sumSquaresX * sumSquaresY);
    }

    /// <summary>
    /// Assigns 1-based ranks, giving tied values the average of the ranks they span.
    /// </summary>
    internal static double[] Rank(IReadOnlyList<double> values)
    {
        var n = values.Count;
        var sortedIndices = Enumerable.Range(0, n).OrderBy(i => values[i]).ToArray();
        var ranks = new double[n];

        var i = 0;
        while (i < n)
        {
            var j = i;
            while (j + 1 < n && values[sortedIndices[j + 1]] == values[sortedIndices[i]])
            {
                j++;
            }

            var averageRank = (i + j) / 2.0 + 1;
            for (var k = i; k <= j; k++)
            {
                ranks[sortedIndices[k]] = averageRank;
            }

            i = j + 1;
        }

        return ranks;
    }
}
