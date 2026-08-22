using System.Collections.Immutable;

namespace DataStructures;

/// <summary>
/// Draw-ready coordinates for one heatmap: computed once by <c>Algorithms.HeatmapLayout.Compute</c>
/// from a <see cref="GraphDrawData"/> and a target area, then consumed purely by drawing code, which
/// does no arithmetic of its own. Unlike <see cref="GraphDrawData"/>, this never crosses the message
/// gateway or gets persisted, so it intentionally has no DTO/serialization support.
/// </summary>
public sealed class HeatmapLayoutData
{
    private HeatmapLayoutData(
        OMLine xAxis, OMLine yAxis,
        ImmutableArray<OMRect> cellRects,
        ImmutableArray<double> xTickX, ImmutableArray<string> xTickText, double xTickY,
        ImmutableArray<double> yTickY, ImmutableArray<string> yTickText, double yTickX,
        string? xAxisTitleText, double xAxisTitleX, double xAxisTitleY,
        string? yAxisTitleText, double yAxisTitleX, double yAxisTitleY)
    {
        // Private constructor to make sure object creation goes through Create().
        XAxis = xAxis;
        YAxis = yAxis;
        CellRects = cellRects;
        XTickX = xTickX;
        XTickText = xTickText;
        XTickY = xTickY;
        YTickY = yTickY;
        YTickText = yTickText;
        YTickX = yTickX;
        XAxisTitleText = xAxisTitleText;
        XAxisTitleX = xAxisTitleX;
        XAxisTitleY = xAxisTitleY;
        YAxisTitleText = yAxisTitleText;
        YAxisTitleX = yAxisTitleX;
        YAxisTitleY = yAxisTitleY;
    }

    public OMLine XAxis { get; }
    public OMLine YAxis { get; }

    /// <summary>Row-major, same indexing as <see cref="GraphDrawData.CellColorsRowMajor"/>.</summary>
    public ImmutableArray<OMRect> CellRects { get; }

    /// <summary>Empty when the source graph's DrawAxisTickLabels is false.</summary>
    public ImmutableArray<double> XTickX { get; }
    public ImmutableArray<string> XTickText { get; }
    public double XTickY { get; }

    public ImmutableArray<double> YTickY { get; }
    public ImmutableArray<string> YTickText { get; }
    public double YTickX { get; }

    /// <summary>Null when the source graph's DrawAxisTitles is false.</summary>
    public string? XAxisTitleText { get; }
    public double XAxisTitleX { get; }
    public double XAxisTitleY { get; }

    public string? YAxisTitleText { get; }
    public double YAxisTitleX { get; }
    public double YAxisTitleY { get; }

    public static Result<HeatmapLayoutData> Create(
        OMLine xAxis, OMLine yAxis,
        ImmutableArray<OMRect> cellRects,
        ImmutableArray<double> xTickX, ImmutableArray<string> xTickText, double xTickY,
        ImmutableArray<double> yTickY, ImmutableArray<string> yTickText, double yTickX,
        string? xAxisTitleText, double xAxisTitleX, double xAxisTitleY,
        string? yAxisTitleText, double yAxisTitleX, double yAxisTitleY)
    {
        if (cellRects.Any(rect => rect.Width <= 0 || rect.Height <= 0))
        {
            return new Failure<HeatmapLayoutData>("The heatmap's drawing area is too small to lay out.");
        }

        if (xTickX.Length != xTickText.Length)
        {
            return new Failure<HeatmapLayoutData>("X-axis tick positions and labels must have the same length.");
        }

        if (yTickY.Length != yTickText.Length)
        {
            return new Failure<HeatmapLayoutData>("Y-axis tick positions and labels must have the same length.");
        }

        return new Success<HeatmapLayoutData>(
            new HeatmapLayoutData(
                xAxis, yAxis,
                cellRects,
                xTickX, xTickText, xTickY,
                yTickY, yTickText, yTickX,
                xAxisTitleText, xAxisTitleX, xAxisTitleY,
                yAxisTitleText, yAxisTitleX, yAxisTitleY));
    }
}
