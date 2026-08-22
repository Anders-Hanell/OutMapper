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
        ImmutableArray<OMTextBox> tickLabels,
        ImmutableArray<OMTextBox> axisTitles)
    {
        // Private constructor to make sure object creation goes through Create().
        XAxis = xAxis;
        YAxis = yAxis;
        CellRects = cellRects;
        TickLabels = tickLabels;
        AxisTitles = axisTitles;
    }

    public OMLine XAxis { get; }
    public OMLine YAxis { get; }

    /// <summary>Row-major, same indexing as <see cref="GraphDrawData.CellColorsRowMajor"/>.</summary>
    public ImmutableArray<OMRect> CellRects { get; }

    /// <summary>Every X-axis and Y-axis tick label. Empty when the source graph's DrawAxisTickLabels is false.</summary>
    public ImmutableArray<OMTextBox> TickLabels { get; }

    /// <summary>0, 1, or 2 entries (X title, Y title) depending on the source graph's DrawAxisTitles flag.</summary>
    public ImmutableArray<OMTextBox> AxisTitles { get; }

    public static Result<HeatmapLayoutData> Create(
        OMLine xAxis, OMLine yAxis,
        ImmutableArray<OMRect> cellRects,
        ImmutableArray<OMTextBox> tickLabels,
        ImmutableArray<OMTextBox> axisTitles)
    {
        if (cellRects.Any(rect => rect.Width <= 0 || rect.Height <= 0))
        {
            return new Failure<HeatmapLayoutData>("The heatmap's drawing area is too small to lay out.");
        }

        if (tickLabels.Any(box => box.Rect.Width <= 0 || box.Rect.Height <= 0)
            || axisTitles.Any(box => box.Rect.Width <= 0 || box.Rect.Height <= 0))
        {
            return new Failure<HeatmapLayoutData>("The heatmap's drawing area is too small to lay out.");
        }

        return new Success<HeatmapLayoutData>(
            new HeatmapLayoutData(xAxis, yAxis, cellRects, tickLabels, axisTitles));
    }
}
