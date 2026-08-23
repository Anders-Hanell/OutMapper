using SkiaSharp;
using DataStructures;

namespace OutMapper;

/// <summary>
/// Paints a Figure's laid-out geometry — the per-cell heatmaps and their letter labels — onto any
/// <see cref="SKCanvas"/>. Used both for the PDF page canvas (<see cref="FigurePdfGenerator"/>) and for
/// an offscreen raster canvas (<see cref="FigurePreviewRenderer"/>), since neither the geometry nor the
/// painting logic cares what kind of surface the canvas is backed by.
/// </summary>
internal static class FigureDrawing
{
    internal static void Paint(SKCanvas canvas, FigureLayoutData layout)
    {
        foreach (var heatmapLayout in layout.HeatmapLayouts)
        {
            HeatmapDrawing.Draw(canvas, heatmapLayout);
        }

        using var labelFont = new SKFont { Size = TextFitting.FitUniformSize(layout.Labels), Embolden = true };

        foreach (var labelBox in layout.Labels)
        {
            DrawLabel(canvas, labelBox, labelFont);
        }
    }

    // Top-right, not top-left: the rotated y-axis title (when present) occupies the cell's left edge
    // and can run taller than the cell, so top-left risks a collision.
    private static void DrawLabel(SKCanvas canvas, OMTextBox labelBox, SKFont labelFont)
    {
        using var labelPaint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true
        };

        canvas.DrawText(
            labelBox.Text, (float)labelBox.Rect.TopRight.X, (float)labelBox.Rect.BottomRight.Y - 2f,
            SKTextAlign.Right, labelFont, labelPaint);
    }
}
