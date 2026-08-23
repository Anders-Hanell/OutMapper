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
        ImmutableArray<OMLine> lines,
        ImmutableArray<OMRect> cellRects,
        ImmutableArray<OMTextGroup> textGroups)
    {
        // Private constructor to make sure object creation goes through Create().
        Lines = lines;
        CellRects = cellRects;
        TextGroups = textGroups;
    }

    /// <summary>The heatmap's axis lines (X then Y) — this class holds layout data, not heatmap-specific
    /// knowledge, so it doesn't track which line is which.</summary>
    public ImmutableArray<OMLine> Lines { get; }

    /// <summary>Row-major, same indexing as <see cref="GraphDrawData.CellColorsRowMajor"/>.</summary>
    public ImmutableArray<OMRect> CellRects { get; }

    /// <summary>
    /// Text to render, grouped by shared font size (e.g. the axis tick labels form one group, the axis
    /// titles another) — this class holds layout data, not heatmap-specific knowledge, so it doesn't
    /// track which group is which. A group may be empty (e.g. when the source graph's DrawAxisTickLabels
    /// or DrawAxisTitles is false).
    /// </summary>
    public ImmutableArray<OMTextGroup> TextGroups { get; }

    public static Result<HeatmapLayoutData> Create(
        ImmutableArray<OMLine> lines,
        ImmutableArray<OMRect> cellRects,
        ImmutableArray<OMTextGroup> textGroups)
    {
        if (cellRects.Any(rect => rect.Width <= 0 || rect.Height <= 0))
        {
            return new Failure<HeatmapLayoutData>("The heatmap's drawing area is too small to lay out.");
        }

        if (textGroups.Any(group => group.Boxes.Any(box => box.Rect.Width <= 0 || box.Rect.Height <= 0)))
        {
            return new Failure<HeatmapLayoutData>("The heatmap's drawing area is too small to lay out.");
        }

        return new Success<HeatmapLayoutData>(
            new HeatmapLayoutData(lines, cellRects, textGroups));
    }
}
