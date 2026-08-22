using System.Collections.Immutable;
using System.Globalization;
using DataStructures;

namespace Algorithms;

/// <summary>
/// Computes where a heatmap's grid cells, tick labels, and axis titles land inside a target area,
/// so that OutMapper's drawing code can do pure painting from already-final coordinates. Skia-free
/// on purpose: OutMapper is not allowed a project reference to Algorithms, so this crosses back into
/// OutMapper only through <c>TaskManager.HeatmapLayoutService</c>.
/// </summary>
public static class HeatmapLayout
{
    private static readonly OMColor AxisColor = new(0, 0, 0);
    private const double AxisLineWidth = 2;

    /// <summary>Height of an X-axis tick label's on-page footprint.</summary>
    private const double XTickLabelMaxHeight = 20.0;

    /// <summary>Width of a Y-axis tick label's on-page footprint.</summary>
    private const double YTickLabelMaxWidth = 26.0;

    /// <summary>Thickness (shared by both axes) of an axis title's on-page footprint.</summary>
    private const double AxisTitleMaxThickness = 26.0;

    public static Result<HeatmapLayoutData> Compute(
        GraphDrawData data, double areaLeft, double areaTop, double areaWidth, double areaHeight)
    {
        var rowCount = data.RowCount;
        var colCount = data.ColCount;

        var axisRight = areaLeft + areaWidth;
        var axisBottom = areaTop + areaHeight;
        var cellWidth = areaWidth / colCount;
        var cellHeight = areaHeight / rowCount;

        var cellRects = new OMRect[rowCount * colCount];
        for (var row = 0; row < rowCount; row++)
        {
            for (var col = 0; col < colCount; col++)
            {
                var index = row * colCount + col;
                var left = areaLeft + col * cellWidth;
                // Row 0 is the lowest channel-B bin, drawn at the bottom of the area.
                var top = axisBottom - (row + 1) * cellHeight;
                var color = ParseCellColor(data.CellColorsRowMajor[index]);

                cellRects[index] = MakeRect(left, top, cellWidth, cellHeight, color);
            }
        }

        var tickLabels = ImmutableArray<OMTextBox>.Empty;
        if (data.DrawAxisTickLabels)
        {
            var builder = ImmutableArray.CreateBuilder<OMTextBox>(colCount + 1 + rowCount + 1);

            for (var col = 0; col <= colCount; col++)
            {
                var xCenter = areaLeft + col * cellWidth;
                var rect = MakeRect(xCenter - cellWidth / 2.0, axisBottom, cellWidth, XTickLabelMaxHeight, AxisColor);
                builder.Add(new OMTextBox(FormatTick(data.ChannelABinEdges[col]), rect, OMTextRotation.Horizontal));
            }

            for (var row = 0; row <= rowCount; row++)
            {
                var yCenter = axisBottom - row * cellHeight;
                var rect = MakeRect(areaLeft - YTickLabelMaxWidth, yCenter - cellHeight / 2.0, YTickLabelMaxWidth, cellHeight, AxisColor);
                builder.Add(new OMTextBox(FormatTick(data.ChannelBBinEdges[row]), rect, OMTextRotation.Horizontal));
            }

            tickLabels = builder.MoveToImmutable();
        }

        var axisTitles = ImmutableArray<OMTextBox>.Empty;
        if (data.DrawAxisTitles)
        {
            var tickLabelBottomOffset = data.DrawAxisTickLabels ? XTickLabelMaxHeight : 0.0;
            var tickLabelLeftOffset = data.DrawAxisTickLabels ? YTickLabelMaxWidth : 0.0;

            var xTitleRect = MakeRect(areaLeft, axisBottom + tickLabelBottomOffset, areaWidth, AxisTitleMaxThickness, AxisColor);
            var yTitleRect = MakeRect(
                areaLeft - tickLabelLeftOffset - AxisTitleMaxThickness, areaTop, AxisTitleMaxThickness, areaHeight, AxisColor);

            axisTitles = ImmutableArray.Create(
                new OMTextBox(data.ChannelAName, xTitleRect, OMTextRotation.Horizontal),
                new OMTextBox(data.ChannelBName, yTitleRect, OMTextRotation.CounterClockwise90));
        }

        var xAxis = new OMLine(new OMPoint(areaLeft, axisBottom), new OMPoint(axisRight, axisBottom), AxisColor, AxisLineWidth);
        var yAxis = new OMLine(new OMPoint(areaLeft, axisBottom), new OMPoint(areaLeft, areaTop), AxisColor, AxisLineWidth);

        return HeatmapLayoutData.Create(xAxis, yAxis, cellRects.ToImmutableArray(), tickLabels, axisTitles);
    }

    /// <summary>
    /// How much space (in the same units as <see cref="Compute"/>'s area) a heatmap's tick labels and
    /// axis titles protrude outside the cell grid, so callers laying out several heatmaps (e.g. a
    /// figure grid) can shrink each cell's rect before calling <see cref="Compute"/>, keeping this
    /// chrome from bleeding into the next cell. Shares the same offset constants as
    /// <see cref="Compute"/> so the two can never drift apart.
    /// </summary>
    public static (double ReservedLeft, double ReservedBottom) ComputeReservedMargins(GraphDrawData data)
    {
        double reservedLeft = 0, reservedBottom = 0;
        if (data.DrawAxisTickLabels)
        {
            reservedBottom += XTickLabelMaxHeight;
            reservedLeft += YTickLabelMaxWidth;
        }

        if (data.DrawAxisTitles)
        {
            reservedBottom += AxisTitleMaxThickness;
            reservedLeft += AxisTitleMaxThickness;
        }

        return (reservedLeft, reservedBottom);
    }

    private static string FormatTick(double value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static OMRect MakeRect(double left, double top, double width, double height, OMColor color) =>
        new(
            new OMPoint(left, top), new OMPoint(left + width, top),
            new OMPoint(left, top + height), new OMPoint(left + width, top + height),
            width, height, color);

    /// <summary>
    /// Parses a "#RRGGBB" hex color, matching the format <c>Algorithms.JetColorScale.ToHexColor</c>
    /// produces. A null or empty string (an NA cell) is rendered as white.
    /// </summary>
    private static OMColor ParseCellColor(string hexColor)
    {
        if (string.IsNullOrEmpty(hexColor))
        {
            return new OMColor(255, 255, 255);
        }

        var hex = hexColor.StartsWith('#') ? hexColor[1..] : hexColor;
        var r = Convert.ToByte(hex[..2], 16);
        var g = Convert.ToByte(hex[2..4], 16);
        var b = Convert.ToByte(hex[4..6], 16);
        return new OMColor(r, g, b);
    }
}
