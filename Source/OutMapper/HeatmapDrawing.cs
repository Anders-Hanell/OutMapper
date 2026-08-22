using DataStructures;
using SkiaSharp;
using TaskManager;

namespace OutMapper;

/// <summary>
/// Draws one N x M colored heatmap grid, with optional axis tick labels and axis titles,
/// into an arbitrary rectangle on an SkiaSharp canvas. Shared by <see cref="AnalysisGraphPdfService"/>
/// (one full-size heatmap per PDF) and <see cref="FigureGraphPdfService"/> (a grid of smaller heatmaps).
/// The actual cell/tick/title geometry is computed by <see cref="HeatmapLayoutService"/> (which forwards
/// to the pure, Skia-free <c>Algorithms.HeatmapLayout</c>); this method only paints.
/// </summary>
internal static class HeatmapDrawing
{
    internal static void Draw(SKCanvas canvas, SKRect graphArea, GraphDrawData data)
    {
        var layoutResult = HeatmapLayoutService.ComputeLayout(
            data, graphArea.Left, graphArea.Top, graphArea.Width, graphArea.Height);

        if (layoutResult is not Success<HeatmapLayoutData> success)
        {
            return;
        }

        var layout = success.Value;

        using var tickFont = new SKFont { Size = TextFitting.FitUniformSize(layout.TickLabels) };
        using var titleFont = new SKFont { Size = TextFitting.FitUniformSize(layout.AxisTitles) };

        // Axes
        DrawLine(canvas, layout.XAxis);
        DrawLine(canvas, layout.YAxis);

        // Colored grid
        foreach (var cellRect in layout.CellRects)
        {
            DrawRect(canvas, cellRect);
        }

        foreach (var textBox in layout.TickLabels)
        {
            DrawTextBox(canvas, textBox, tickFont);
        }

        foreach (var textBox in layout.AxisTitles)
        {
            DrawTextBox(canvas, textBox, titleFont);
        }
    }

    private static void DrawLine(SKCanvas canvas, OMLine line)
    {
        using var paint = new SKPaint
        {
            Color = new SKColor(line.LineColor.R, line.LineColor.G, line.LineColor.B),
            StrokeWidth = (float)line.LineWidth,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };

        canvas.DrawLine((float)line.Start.X, (float)line.Start.Y, (float)line.End.X, (float)line.End.Y, paint);
    }

    private static void DrawRect(SKCanvas canvas, OMRect rect)
    {
        using var paint = new SKPaint
        {
            Color = new SKColor(rect.FillColor.R, rect.FillColor.G, rect.FillColor.B),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        var skRect = new SKRect(
            (float)rect.TopLeft.X, (float)rect.TopLeft.Y,
            (float)rect.BottomRight.X, (float)rect.BottomRight.Y);
        canvas.DrawRect(skRect, paint);
    }

    /// <summary>
    /// Draws Text centered (both horizontally and vertically) within Rect, rotating around the rect's
    /// center for a <see cref="OMTextRotation.CounterClockwise90"/> box — Rect already describes that
    /// box's rotated on-page footprint, so its center is the correct pivot either way.
    /// </summary>
    private static void DrawTextBox(SKCanvas canvas, OMTextBox box, SKFont font)
    {
        using var paint = new SKPaint
        {
            Color = new SKColor(box.Rect.FillColor.R, box.Rect.FillColor.G, box.Rect.FillColor.B),
            IsAntialias = true
        };

        var centerX = (float)(box.Rect.TopLeft.X + box.Rect.Width / 2.0);
        var centerY = (float)(box.Rect.TopLeft.Y + box.Rect.Height / 2.0);

        font.MeasureText(box.Text, out var bounds);

        canvas.Save();
        canvas.Translate(centerX, centerY);
        if (box.Rotation == OMTextRotation.CounterClockwise90)
        {
            canvas.RotateDegrees(-90);
        }

        canvas.DrawText(box.Text, 0, -bounds.MidY, SKTextAlign.Center, font, paint);
        canvas.Restore();
    }
}
