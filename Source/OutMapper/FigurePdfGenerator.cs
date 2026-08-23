using SkiaSharp;
using DataStructures;
using Messages;

namespace OutMapper;

/// <summary>
/// Draws a Figure's PDF bytes in memory — a grid of the Figure's assigned Analysis heatmaps, one small
/// heatmap per cell, with a letter caption on every cell when the Figure has a <see cref="FigureLabelStyle"/>.
/// Knows nothing about files or paths; <see cref="FigureGraphPdfService"/> is what writes the bytes this
/// produces to disk. Returns null if the figure can't be laid out.
/// </summary>
internal static class FigurePdfGenerator
{
    internal static async Task<byte[]?> GenerateAsync(FigureDrawData figure)
    {
        var request = new ComputeFigureLayoutRequest(Guid.NewGuid(), figure);
        var response = await GatewayRequestCorrelator.SendAsync<ComputeFigureLayoutRequest, ComputeFigureLayoutResponse>(request);
        if (response.Result is not Success<FigureLayoutData> layoutSuccess)
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
