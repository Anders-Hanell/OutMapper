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

                cellRects[index] = new OMRect(
                    new OMPoint(left, top), new OMPoint(left + cellWidth, top),
                    new OMPoint(left, top + cellHeight), new OMPoint(left + cellWidth, top + cellHeight),
                    cellWidth, cellHeight, color);
            }
        }

        var xTickX = ImmutableArray<double>.Empty;
        var xTickText = ImmutableArray<string>.Empty;
        var yTickY = ImmutableArray<double>.Empty;
        var yTickText = ImmutableArray<string>.Empty;
        if (data.DrawAxisTickLabels)
        {
            var xTicks = new double[colCount + 1];
            var xTexts = new string[colCount + 1];
            for (var col = 0; col <= colCount; col++)
            {
                xTicks[col] = areaLeft + col * cellWidth;
                xTexts[col] = FormatTick(data.ChannelABinEdges[col]);
            }

            xTickX = xTicks.ToImmutableArray();
            xTickText = xTexts.ToImmutableArray();

            var yTicks = new double[rowCount + 1];
            var yTexts = new string[rowCount + 1];
            for (var row = 0; row <= rowCount; row++)
            {
                yTicks[row] = axisBottom - row * cellHeight + 4.0;
                yTexts[row] = FormatTick(data.ChannelBBinEdges[row]);
            }

            yTickY = yTicks.ToImmutableArray();
            yTickText = yTexts.ToImmutableArray();
        }

        string? xAxisTitleText = null;
        string? yAxisTitleText = null;
        double xAxisTitleX = 0, xAxisTitleY = 0, yAxisTitleX = 0, yAxisTitleY = 0;
        if (data.DrawAxisTitles)
        {
            xAxisTitleText = data.ChannelAName;
            xAxisTitleX = (areaLeft + axisRight) / 2.0;
            xAxisTitleY = axisBottom + 36.0;

            yAxisTitleText = data.ChannelBName;
            yAxisTitleX = areaLeft - 40.0;
            yAxisTitleY = (areaTop + axisBottom) / 2.0;
        }

        var xAxis = new OMLine(new OMPoint(areaLeft, axisBottom), new OMPoint(axisRight, axisBottom), AxisColor, AxisLineWidth);
        var yAxis = new OMLine(new OMPoint(areaLeft, axisBottom), new OMPoint(areaLeft, areaTop), AxisColor, AxisLineWidth);

        return HeatmapLayoutData.Create(
            xAxis, yAxis,
            cellRects.ToImmutableArray(),
            xTickX, xTickText, axisBottom + 14.0,
            yTickY, yTickText, areaLeft - 6.0,
            xAxisTitleText, xAxisTitleX, xAxisTitleY,
            yAxisTitleText, yAxisTitleX, yAxisTitleY);
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
            reservedBottom += 20.0;
            reservedLeft += 26.0;
        }

        if (data.DrawAxisTitles)
        {
            reservedBottom += 26.0;
            reservedLeft += 26.0;
        }

        return (reservedLeft, reservedBottom);
    }

    private static string FormatTick(double value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

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
