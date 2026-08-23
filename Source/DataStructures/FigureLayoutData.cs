using System.Collections.Immutable;

namespace DataStructures;

/// <summary>
/// Draw-ready coordinates for one figure grid: computed once by <c>Algorithms.FigureLayout.Compute</c>
/// from a <see cref="FigureDrawData"/>, then consumed purely by drawing code, which does no arithmetic
/// of its own — including the PDF page size the grid was laid out onto. Unlike <see cref="FigureDrawData"/>,
/// this never crosses the message gateway or gets persisted, so it intentionally has no DTO/serialization
/// support.
/// </summary>
public sealed class FigureLayoutData
{
    private FigureLayoutData(
        double pageWidth, double pageHeight,
        ImmutableArray<HeatmapLayoutData> heatmapLayouts,
        ImmutableArray<OMTextBox> labels)
    {
        // Private constructor to make sure object creation goes through Create().
        PageWidth = pageWidth;
        PageHeight = pageHeight;
        HeatmapLayouts = heatmapLayouts;
        Labels = labels;
    }

    /// <summary>Width of the PDF page the grid was laid out onto.</summary>
    public double PageWidth { get; }

    /// <summary>Height of the PDF page the grid was laid out onto.</summary>
    public double PageHeight { get; }

    /// <summary>
    /// The fully computed heatmap geometry for each assigned graph, already positioned at its final
    /// on-page location, same order as <see cref="FigureDrawData.Graphs"/>. Each entry already carries
    /// its own graph's cell colors, axes, tick labels, and axis titles, so drawing code doesn't need to
    /// go back to the source <see cref="GraphDrawData"/> or recompute anything.
    /// </summary>
    public ImmutableArray<HeatmapLayoutData> HeatmapLayouts { get; }

    /// <summary>
    /// One letter-label box per grid cell, in row-major order over the whole grid — graph cells and
    /// empty cells alike, since each box already carries its own absolute on-page position and drawing
    /// one doesn't depend on what else is (or isn't) painted into that cell. Empty when the source
    /// figure's LabelStyle is <see cref="FigureLabelStyle.None"/>.
    /// </summary>
    public ImmutableArray<OMTextBox> Labels { get; }

    public static Result<FigureLayoutData> Create(
        double pageWidth, double pageHeight,
        ImmutableArray<HeatmapLayoutData> heatmapLayouts,
        ImmutableArray<OMTextBox> labels)
    {
        if (pageWidth <= 0 || pageHeight <= 0)
        {
            return new Failure<FigureLayoutData>("The figure's page must have a positive width and height.");
        }

        if (labels.Any(box => box.Rect.Width <= 0 || box.Rect.Height <= 0))
        {
            return new Failure<FigureLayoutData>("The figure's drawing area is too small to lay out.");
        }

        return new Success<FigureLayoutData>(new FigureLayoutData(pageWidth, pageHeight, heatmapLayouts, labels));
    }
}
