using SkiaSharp;
using DataStructures;

namespace TaskManager;

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

        FigureDrawing.Paint(canvas, layout);

        document.EndPage();
        document.Close();

        return stream.ToArray();
    }
}
