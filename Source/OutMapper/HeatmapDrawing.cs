using System.Globalization;
using DataStructures;
using SkiaSharp;

namespace OutMapper;

/// <summary>
/// Draws one N x M colored heatmap grid, with optional axis tick labels and axis titles,
/// into an arbitrary rectangle on an SkiaSharp canvas. Shared by <see cref="AnalysisGraphPdfService"/>
/// (one full-size heatmap per PDF) and <see cref="FigureGraphPdfService"/> (a grid of smaller heatmaps).
/// </summary>
internal static class HeatmapDrawing
{
    internal static void Draw(SKCanvas canvas, SKRect graphArea, GraphDrawData data)
    {
        var rowCount = data.RowCount;
        var colCount = data.ColCount;

        var axisLeft = graphArea.Left;
        var axisTop = graphArea.Top;
        var axisRight = graphArea.Right;
        var axisBottom = graphArea.Bottom;
        var cellWidth = graphArea.Width / colCount;
        var cellHeight = graphArea.Height / rowCount;

        using var axisPaint = new SKPaint
        {
            Color = SKColors.Black,
            StrokeWidth = 2,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };

        using var gridLinePaint = new SKPaint
        {
            Color = SKColors.Gray,
            StrokeWidth = 1,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };

        using var fillPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        using var labelPaint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true
        };

        using var tickFont = new SKFont { Size = 10 };
        using var titleFont = new SKFont { Size = 18 };

        // Axes
        canvas.DrawLine(axisLeft, axisBottom, axisRight, axisBottom, axisPaint);
        canvas.DrawLine(axisLeft, axisBottom, axisLeft, axisTop, axisPaint);

        // Colored grid. Row 0 is the lowest channel-B bin, drawn at the bottom of the area.
        for (var row = 0; row < rowCount; row++)
        {
            for (var col = 0; col < colCount; col++)
            {
                var hexColor = data.CellColorsRowMajor[row * colCount + col];
                fillPaint.Color = string.IsNullOrEmpty(hexColor) ? SKColors.White : SKColor.Parse(hexColor);

                var left = axisLeft + col * cellWidth;
                var top = axisBottom - (row + 1) * cellHeight;
                var rect = new SKRect(left, top, left + cellWidth, top + cellHeight);
                canvas.DrawRect(rect, fillPaint);
                canvas.DrawRect(rect, gridLinePaint);
            }
        }

        if (data.DrawAxisTickLabels)
        {
            for (var col = 0; col <= colCount; col++)
            {
                var x = axisLeft + col * cellWidth;
                canvas.DrawText(FormatTick(data.ChannelABinEdges[col]), x, axisBottom + 14f, SKTextAlign.Center, tickFont, labelPaint);
            }

            for (var row = 0; row <= rowCount; row++)
            {
                var y = axisBottom - row * cellHeight;
                canvas.DrawText(FormatTick(data.ChannelBBinEdges[row]), axisLeft - 6f, y + 4f, SKTextAlign.Right, tickFont, labelPaint);
            }
        }

        if (data.DrawAxisTitles)
        {
            canvas.DrawText(data.ChannelAName, (axisLeft + axisRight) / 2f, axisBottom + 36f, SKTextAlign.Center, titleFont, labelPaint);

            canvas.Save();
            canvas.Translate(axisLeft - 40f, (axisTop + axisBottom) / 2f);
            canvas.RotateDegrees(-90);
            canvas.DrawText(data.ChannelBName, 0, 0, SKTextAlign.Center, titleFont, labelPaint);
            canvas.Restore();
        }
    }

    private static string FormatTick(double value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
