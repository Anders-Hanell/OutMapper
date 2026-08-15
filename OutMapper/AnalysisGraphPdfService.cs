using SkiaSharp;
using System.Globalization;
using System.IO;
using Messages;
using Path = System.IO.Path;

namespace OutMapper;

internal static class AnalysisGraphPdfService
{
    /// <summary>
    /// Draws the association grid from a <see cref="GenerateAnalysisGraphResponse"/> as an N x M
    /// colored heatmap and writes it to the project's Output folder as "&lt;analysisName&gt;.pdf".
    /// Returns the written file path, or null if it could not be written.
    /// </summary>
    public static string? GeneratePdf(string projectName, string analysisName, GenerateAnalysisGraphResponse graph)
    {
        var workspaceFolder = SettingsWorkspaceContent.LoadWorkspaceFolderPath();
        if (string.IsNullOrWhiteSpace(workspaceFolder))
        {
            return null;
        }

        var rowCount = graph.ChannelBBinEdges.Length - 1;
        var colCount = graph.ChannelABinEdges.Length - 1;
        if (rowCount <= 0 || colCount <= 0)
        {
            return null;
        }

        var outputFolder = Path.Combine(workspaceFolder, "Projects", projectName, ProjectFolderService.ProjectOutputFolderName);
        Directory.CreateDirectory(outputFolder);
        var outputFile = Path.Combine(outputFolder, analysisName + ".pdf");

        using var stream = File.OpenWrite(outputFile);
        using var document = SKDocument.CreatePdf(stream);
        if (document is null)
        {
            return null;
        }

        const float pageWidth = 612f;
        const float pageHeight = 792f;
        const float margin = 72f;
        const float graphSize = 360f;
        const float axisLeft = margin + 40f;
        const float axisBottom = pageHeight - margin - 24f;
        const float axisTop = axisBottom - graphSize;
        const float axisRight = axisLeft + graphSize;
        var cellWidth = graphSize / colCount;
        var cellHeight = graphSize / rowCount;

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

        using var font = new SKFont { Size = 18 };
        using var tickFont = new SKFont { Size = 10 };

        // Axes
        canvas.DrawLine(axisLeft, axisBottom, axisRight, axisBottom, axisPaint);
        canvas.DrawLine(axisLeft, axisBottom, axisLeft, axisTop, axisPaint);

        // Colored grid. Row 0 is the lowest channel-B bin, drawn at the bottom of the page.
        for (var row = 0; row < rowCount; row++)
        {
            for (var col = 0; col < colCount; col++)
            {
                var hexColor = graph.CellColorsRowMajor[row * colCount + col];
                fillPaint.Color = string.IsNullOrEmpty(hexColor) ? SKColors.White : SKColor.Parse(hexColor);

                var left = axisLeft + col * cellWidth;
                var top = axisBottom - (row + 1) * cellHeight;
                var rect = new SKRect(left, top, left + cellWidth, top + cellHeight);
                canvas.DrawRect(rect, fillPaint);
                canvas.DrawRect(rect, gridLinePaint);
            }
        }

        // Axis tick labels, one per bin edge.
        for (var col = 0; col <= colCount; col++)
        {
            var x = axisLeft + col * cellWidth;
            canvas.DrawText(FormatTick(graph.ChannelABinEdges[col]), x, axisBottom + 14f, SKTextAlign.Center, tickFont, labelPaint);
        }

        for (var row = 0; row <= rowCount; row++)
        {
            var y = axisBottom - row * cellHeight;
            canvas.DrawText(FormatTick(graph.ChannelBBinEdges[row]), axisLeft - 6f, y + 4f, SKTextAlign.Right, tickFont, labelPaint);
        }

        // Axis titles
        canvas.DrawText(graph.ChannelAName, (axisLeft + axisRight) / 2f, axisBottom + 36f, SKTextAlign.Center, font, labelPaint);

        canvas.Save();
        canvas.Translate(axisLeft - 40f, (axisTop + axisBottom) / 2f);
        canvas.RotateDegrees(-90);
        canvas.DrawText(graph.ChannelBName, 0, 0, SKTextAlign.Center, font, labelPaint);
        canvas.Restore();

        // Graph title
        using var titlePaint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true
        };

        using var titleFont = new SKFont { Size = 24 };
        canvas.DrawText(analysisName, pageWidth / 2f, margin, SKTextAlign.Center, titleFont, titlePaint);

        document.EndPage();
        document.Close();

        return outputFile;
    }

    private static string FormatTick(double value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
