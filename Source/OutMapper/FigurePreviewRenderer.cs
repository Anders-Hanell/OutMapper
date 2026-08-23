using SkiaSharp;
using DataStructures;
using TaskManager;

namespace OutMapper;

/// <summary>
/// Rasterizes a Figure to an in-memory PNG for the on-screen live preview, reusing the same layout
/// computation and paint routine as <see cref="FigurePdfGenerator"/>. Returns null if the figure can't
/// be laid out.
/// </summary>
internal static class FigurePreviewRenderer
{
    internal static byte[]? RenderPng(FigureDrawData figure, int maxDimensionPx)
    {
        var layoutResult = FigureLayoutService.ComputeLayout(figure);
        if (layoutResult is not Success<FigureLayoutData> layoutSuccess)
        {
            return null;
        }

        var layout = layoutSuccess.Value;
        var scale = maxDimensionPx / Math.Max(layout.PageWidth, layout.PageHeight);
        var width = Math.Max(1, (int)Math.Round(layout.PageWidth * scale));
        var height = Math.Max(1, (int)Math.Round(layout.PageHeight * scale));

        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);
        canvas.Scale((float)scale);

        FigureDrawing.Paint(canvas, layout);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
