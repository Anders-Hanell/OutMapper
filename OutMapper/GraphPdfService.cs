using SkiaSharp;
using System.IO;
using Path = System.IO.Path;

namespace OutMapper;

internal static class GraphPdfService
{
    public static void GeneratePdf()
    {
        var workspaceFolder = SettingsWorkspaceContent.LoadWorkspaceFolderPath();
        if (string.IsNullOrWhiteSpace(workspaceFolder))
        {
            return;
        }

        var outputFile = Path.Combine(workspaceFolder, "Graph.pdf");

        using var stream = File.OpenWrite(outputFile);
        using var document = SKDocument.CreatePdf(stream);
        if (document is null)
        {
            return;
        }

        const float pageWidth = 612f;
        const float pageHeight = 792f;
        const float margin = 72f;
        const float graphSize = 360f;
        const float axisLeft = margin + 40f;
        const float axisBottom = pageHeight - margin - 24f;
        const float axisTop = axisBottom - graphSize;
        const float axisRight = axisLeft + graphSize;
        const float cellSize = graphSize / 3f;

        using var canvas = document.BeginPage(pageWidth, pageHeight);
        canvas.Clear(SKColors.White);

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

        using var font = new SKFont
        {
            Size = 18
        };

        // Axes
        canvas.DrawLine(axisLeft, axisBottom, axisRight, axisBottom, axisPaint);
        canvas.DrawLine(axisLeft, axisBottom, axisLeft, axisTop, axisPaint);

        // Colored 3x3 grid
        var gridColors = new[]
        {
            SKColors.SkyBlue,
            SKColors.MediumSeaGreen,
            SKColors.PeachPuff,
            SKColors.LightSteelBlue,
            SKColors.LemonChiffon,
            SKColors.Plum,
            SKColors.LightCoral,
            SKColors.PaleGreen,
            SKColors.Khaki
        };

        for (var row = 0; row < 3; row++)
        {
            for (var col = 0; col < 3; col++)
            {
                var squareIndex = row * 3 + col;
                fillPaint.Color = gridColors[squareIndex];
                var left = axisLeft + col * cellSize;
                var top = axisTop + row * cellSize;
                var rect = new SKRect(left, top, left + cellSize, top + cellSize);
                canvas.DrawRect(rect, fillPaint);
                canvas.DrawRect(rect, gridLinePaint);
            }
        }

        // Grid lines
        for (var line = 1; line < 3; line++)
        {
            var x = axisLeft + line * cellSize;
            canvas.DrawLine(x, axisTop, x, axisBottom, gridLinePaint);
            var y = axisTop + line * cellSize;
            canvas.DrawLine(axisLeft, y, axisRight, y, gridLinePaint);
        }

        // Axis titles
        canvas.DrawText("ICP", (axisLeft + axisRight) / 2f, axisBottom + 36f, SKTextAlign.Center, font, labelPaint);

        canvas.Save();
        canvas.Translate(axisLeft - 40f, (axisTop + axisBottom) / 2f);
        canvas.RotateDegrees(-90);
        canvas.DrawText("PRx", 0, 0, SKTextAlign.Center, font, labelPaint);
        canvas.Restore();

        // Graph title
        using var titlePaint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true
        };

        using var titleFont = new SKFont { Size = 24 };
        canvas.DrawText("Sample Graph", pageWidth / 2f, margin, SKTextAlign.Center, titleFont, titlePaint);

        document.EndPage();
        document.Close();
    }
}
