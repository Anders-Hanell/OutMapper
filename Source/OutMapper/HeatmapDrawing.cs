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

        using var labelPaint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true
        };

        using var tickFont = new SKFont { Size = 10 };
        using var titleFont = new SKFont { Size = 18 };

        // Axes
        DrawLine(canvas, layout.XAxis);
        DrawLine(canvas, layout.YAxis);

        // Colored grid
        foreach (var cellRect in layout.CellRects)
        {
            DrawRect(canvas, cellRect);
        }

        for (var i = 0; i < layout.XTickX.Length; i++)
        {
            canvas.DrawText(layout.XTickText[i], (float)layout.XTickX[i], (float)layout.XTickY, SKTextAlign.Center, tickFont, labelPaint);
        }

        for (var i = 0; i < layout.YTickY.Length; i++)
        {
            canvas.DrawText(layout.YTickText[i], (float)layout.YTickX, (float)layout.YTickY[i], SKTextAlign.Right, tickFont, labelPaint);
        }

        if (layout.XAxisTitleText is not null)
        {
            canvas.DrawText(layout.XAxisTitleText, (float)layout.XAxisTitleX, (float)layout.XAxisTitleY, SKTextAlign.Center, titleFont, labelPaint);
        }

        if (layout.YAxisTitleText is not null)
        {
            canvas.Save();
            canvas.Translate((float)layout.YAxisTitleX, (float)layout.YAxisTitleY);
            canvas.RotateDegrees(-90);
            canvas.DrawText(layout.YAxisTitleText, 0, 0, SKTextAlign.Center, titleFont, labelPaint);
            canvas.Restore();
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
}
