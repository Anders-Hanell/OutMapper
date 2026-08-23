using SkiaSharp;
using DataStructures;
using TaskManager;

namespace OutMapper;

/// <summary>
/// Draws a Figure's PDF bytes in memory — a grid of the Figure's assigned Analysis heatmaps, one small
/// heatmap per cell, with a letter caption on every cell when the Figure has a <see cref="FigureLabelStyle"/>.
/// Knows nothing about files or paths; <see cref="FigureGraphPdfService"/> is what writes the bytes this
/// produces to disk. Returns null if the figure can't be laid out.
/// </summary>
internal static class FigurePdfGenerator
{
    internal static byte[]? Generate(FigureDrawData figure)
    {
        var layoutResult = FigureLayoutService.ComputeLayout(figure);
        if (layoutResult is not Success<FigureLayoutData> layoutSuccess)
        {
            return null;
        }

        var layout = layoutSuccess.Value;

        using var stream = new MemoryStream();
        using var document = SKDocument.CreatePdf(stream);
        if (document is null)
        {
            return null;
        }

        using var canvas = document.BeginPage((float)layout.PageWidth, (float)layout.PageHeight);
        canvas.Clear(SKColors.White);

        foreach (var heatmapLayout in layout.HeatmapLayouts)
        {
            HeatmapDrawing.Draw(canvas, heatmapLayout);
        }

        if (!layout.Labels.IsEmpty)
        {
            using var labelPaint = new SKPaint
            {
                Color = SKColors.Black,
                IsAntialias = true
            };

            using var labelFont = new SKFont { Size = 14, Embolden = true };

            foreach (var labelBox in layout.Labels)
            {
                DrawLabel(canvas, labelBox, labelFont, labelPaint);
            }
        }

        document.EndPage();
        document.Close();

        return stream.ToArray();
    }

    // Top-right, not top-left: the rotated y-axis title (when present) occupies the cell's left edge
    // and can run taller than the cell, so top-left risks a collision.
    private static void DrawLabel(SKCanvas canvas, OMTextBox labelBox, SKFont labelFont, SKPaint labelPaint) =>
        canvas.DrawText(
            labelBox.Text, (float)labelBox.Rect.TopRight.X, (float)labelBox.Rect.BottomRight.Y - 2f,
            SKTextAlign.Right, labelFont, labelPaint);
}
