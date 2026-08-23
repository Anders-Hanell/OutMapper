using DataStructures;
using SkiaSharp;
using TaskManager;

namespace OutMapper;

/// <summary>
/// Draws one N x M colored heatmap grid, with optional axis tick labels and axis titles,
/// into an arbitrary rectangle on an SkiaSharp canvas. Shared by <see cref="AnalysisGraphPdfService"/>
/// (one full-size heatmap per PDF) and <see cref="FigureGraphPdfService"/> (a grid of smaller heatmaps).
/// The actual cell/tick/title geometry is computed by <see cref="HeatmapLayoutService"/> (which forwards
/// to the pure, Skia-free <c>Algorithms.HeatmapLayout</c>); this class only paints.
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

        Draw(canvas, success.Value);
    }

    /// <summary>
    /// Paints from an already-computed <see cref="HeatmapLayoutData"/> — no arithmetic, no re-derivation
    /// of the layout. Used directly by <see cref="FigureGraphPdfService"/>, which gets one of these per
    /// grid cell from <c>Algorithms.FigureLayout.Compute</c> instead of a plain draw rect, so a figure's
    /// heatmaps are laid out once rather than once for sizing and again here for painting.
    /// </summary>
    internal static void Draw(SKCanvas canvas, HeatmapLayoutData layout)
    {
        // Axes
        foreach (var line in layout.Lines)
        {
            DrawLine(canvas, line);
        }

        // Colored grid
        foreach (var cellRect in layout.CellRects)
        {
            DrawRect(canvas, cellRect);
        }

        foreach (var group in layout.TextGroups)
        {
            DrawTextGroup(canvas, group);
        }
    }

    // Every box in a group shares one font size (the smallest fit across the group), regardless of
    // what the group's text represents - that's the whole point of grouping them.
    private static void DrawTextGroup(SKCanvas canvas, OMTextGroup group)
    {
        using var font = new SKFont { Size = TextFitting.FitUniformSize(group.Boxes) };

        foreach (var textBox in group.Boxes)
        {
            DrawTextBox(canvas, textBox, font);
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
