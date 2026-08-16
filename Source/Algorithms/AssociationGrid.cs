namespace Algorithms;

public static class AssociationGrid
{
    /// <summary>
    /// Placeholder minimum patient count per cell, not yet exposed as a setting.
    /// </summary>
    public const int MinimumPatientsPerCell = 3;

    /// <summary>
    /// Computes the per-cell Spearman correlation between each patient's percent time in that cell
    /// and their outcome. <paramref name="patientPercentMatrices"/> and <paramref name="patientOutcomes"/>
    /// must be the same length and index-aligned (one patient per position). A cell with fewer than
    /// <see cref="MinimumPatientsPerCell"/> patients is left null (renders as NA/white).
    /// </summary>
    public static double?[,] Compute(
        IReadOnlyList<double[,]> patientPercentMatrices,
        IReadOnlyList<double> patientOutcomes,
        int rowCount,
        int colCount)
    {
        var result = new double?[rowCount, colCount];

        if (patientPercentMatrices.Count < MinimumPatientsPerCell)
        {
            return result;
        }

        var percents = new double[patientPercentMatrices.Count];
        for (var row = 0; row < rowCount; row++)
        {
            for (var col = 0; col < colCount; col++)
            {
                for (var p = 0; p < patientPercentMatrices.Count; p++)
                {
                    percents[p] = patientPercentMatrices[p][row, col];
                }

                result[row, col] = SpearmanCorrelation.Compute(percents, patientOutcomes);
            }
        }

        return result;
    }
}
