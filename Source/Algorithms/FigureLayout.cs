using System.Collections.Immutable;
using System.Text;
using DataStructures;

namespace Algorithms;

/// <summary>
/// Computes where a figure's PDF page, grid cells, per-cell heatmaps, and letter labels land, so that
/// OutMapper's drawing code can do pure painting from already-final coordinates. Skia-free on purpose:
/// OutMapper is not allowed a project reference to Algorithms, so this crosses back into OutMapper only
/// through <c>TaskManager.FigureLayoutService</c>.
/// </summary>
public static class FigureLayout
{
    private static readonly OMColor RectColor = new(0, 0, 0);

    private const double PageWidth = 612.0;
    private const double PageHeight = 792.0;
    private const double Margin = 72.0;

    /// <summary>Side length of the square area the whole grid is laid out into.</summary>
    private const double GridSize = 360.0;

    private const double GridLeft = Margin + 40.0;
    private const double GridBottom = PageHeight - Margin - 24.0;
    private const double GridTop = GridBottom - GridSize;

    /// <summary>Gap between adjacent grid cells.</summary>
    private const double CellGap = 16.0;

    /// <summary>Height of the letter-label strip reserved at the top of a cell.</summary>
    private const double LabelHeight = 16.0;

    public static Result<FigureLayoutData> Compute(FigureDrawData figure)
    {
        var colCount = figure.ColCount;
        var hasLabels = figure.LabelStyle != FigureLabelStyle.None;

        var cellOuterWidth = (GridSize - (colCount - 1) * CellGap) / colCount;
        var cellOuterHeight = (GridSize - (figure.RowCount - 1) * CellGap) / figure.RowCount;

        var heatmapLayouts = ImmutableArray.CreateBuilder<HeatmapLayoutData>();
        var labels = ImmutableArray.CreateBuilder<OMTextBox>();

        var graphIndex = 0;
        for (var index = 0; index < figure.CellHasGraph.Length; index++)
        {
            var row = index / colCount;
            var col = index % colCount;
            var outerLeft = GridLeft + col * (cellOuterWidth + CellGap);
            var outerTop = GridTop + row * (cellOuterHeight + CellGap);

            // Every cell gets a letter in one continuous row-major sequence, whether or not it has a
            // graph, so the index driving the label text is the cell's own position, not a count of
            // graphs seen so far.
            if (hasLabels)
            {
                var label = GetLetterLabel(index, uppercase: figure.LabelStyle == FigureLabelStyle.Uppercase);
                var labelRect = MakeRect(outerLeft, outerTop, cellOuterWidth, LabelHeight, RectColor);
                labels.Add(new OMTextBox(label, labelRect, OMTextRotation.Horizontal));
            }

            if (!figure.CellHasGraph[index])
            {
                continue;
            }

            var cellGraph = figure.Graphs[graphIndex];

            // HeatmapDrawing draws tick labels and axis titles outside the rect it's given, so each
            // cell must shrink its own rect to keep that chrome from bleeding into the next cell.
            var (reservedLeft, reservedBottom) = HeatmapLayout.ComputeReservedMargins(cellGraph);

            var heatmapTop = hasLabels ? outerTop + LabelHeight : outerTop;
            var heatmapLayoutResult = HeatmapLayout.Compute(
                cellGraph,
                outerLeft + reservedLeft, heatmapTop,
                cellOuterWidth - reservedLeft, outerTop + cellOuterHeight - heatmapTop - reservedBottom);

            if (heatmapLayoutResult is not Success<HeatmapLayoutData> heatmapLayoutSuccess)
            {
                return new Failure<FigureLayoutData>("The figure's drawing area is too small to lay out.");
            }

            heatmapLayouts.Add(heatmapLayoutSuccess.Value);
            graphIndex++;
        }

        return FigureLayoutData.Create(
            PageWidth, PageHeight, heatmapLayouts.ToImmutable(), labels.ToImmutable());
    }

    /// <summary>
    /// Spreadsheet-style column label for a zero-based index: 0 -> "A", 25 -> "Z", 26 -> "AA", and so on,
    /// so a figure with more than 26 labeled graphs still gets distinct, alphabetically ordered labels.
    /// </summary>
    private static string GetLetterLabel(int index, bool uppercase)
    {
        var n = index + 1;
        var builder = new StringBuilder();
        while (n > 0)
        {
            n--;
            builder.Insert(0, (char)('A' + n % 26));
            n /= 26;
        }

        var label = builder.ToString();
        return uppercase ? label : label.ToLowerInvariant();
    }

    private static OMRect MakeRect(double left, double top, double width, double height, OMColor color) =>
        new(
            new OMPoint(left, top), new OMPoint(left + width, top),
            new OMPoint(left, top + height), new OMPoint(left + width, top + height),
            width, height, color);
}
