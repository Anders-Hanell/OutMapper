using SkiaSharp;
using System.IO;
using Messages;
using Path = System.IO.Path;

namespace OutMapper;

internal static class FigureGraphPdfService
{
    /// <summary>
    /// Draws a grid of the Figure's assigned Analysis heatmaps, one small heatmap per cell, and writes
    /// it to the project's Output folder as "&lt;figureName&gt;.pdf". Cells with no assigned graph are
    /// left blank. Returns the written file path, or null if it could not be written.
    /// </summary>
    public static string? GeneratePdf(string projectName, string figureName, CreateFigureGraphResponse graph)
    {
        var workspaceFolder = SettingsWorkspaceContent.LoadWorkspaceFolderPath();
        if (string.IsNullOrWhiteSpace(workspaceFolder))
        {
            return null;
        }

        if (graph.RowCount <= 0 || graph.ColCount <= 0)
        {
            return null;
        }

        var outputFolder = Path.Combine(workspaceFolder, "Projects", projectName, ProjectFolderService.ProjectOutputFolderName);
        Directory.CreateDirectory(outputFolder);
        var outputFile = Path.Combine(outputFolder, figureName + ".pdf");

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
        const float gridLeft = margin + 40f;
        const float gridBottom = pageHeight - margin - 24f;
        const float gridTop = gridBottom - graphSize;
        const float captionHeight = 14f;

        var cellOuterWidth = graphSize / graph.ColCount;
        var cellOuterHeight = graphSize / graph.RowCount;

        using var canvas = document.BeginPage(pageWidth, pageHeight);
        canvas.Clear(SKColors.White);

        using var borderPaint = new SKPaint
        {
            Color = SKColors.Gray,
            StrokeWidth = 1,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };

        using var captionPaint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true
        };

        using var captionFont = new SKFont { Size = 9 };

        foreach (var cell in graph.Cells)
        {
            var outerLeft = gridLeft + cell.Col * cellOuterWidth;
            var outerTop = gridTop + cell.Row * cellOuterHeight;
            var outerRect = new SKRect(outerLeft, outerTop, outerLeft + cellOuterWidth, outerTop + cellOuterHeight - captionHeight);

            if (cell.HasGraph)
            {
                var heatmapData = new HeatmapData(
                    cell.ChannelABinEdges, cell.ChannelBBinEdges, cell.CellColorsRowMajor,
                    cell.ChannelAName ?? string.Empty, cell.ChannelBName ?? string.Empty);
                HeatmapDrawing.Draw(canvas, outerRect, heatmapData, drawAxisTickLabels: false, drawAxisTitles: false);

                canvas.DrawText(
                    cell.AnalysisName ?? string.Empty, (outerRect.Left + outerRect.Right) / 2f, outerRect.Bottom + captionHeight - 2f,
                    SKTextAlign.Center, captionFont, captionPaint);
            }
            else
            {
                canvas.DrawRect(outerRect, borderPaint);
            }
        }

        // Figure title
        using var titlePaint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true
        };

        using var titleFont = new SKFont { Size = 24 };
        canvas.DrawText(figureName, pageWidth / 2f, margin, SKTextAlign.Center, titleFont, titlePaint);

        document.EndPage();
        document.Close();

        return outputFile;
    }
}
