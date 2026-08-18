using SkiaSharp;
using System.IO;
using Messages;
using TaskManager;
using Path = System.IO.Path;

namespace OutMapper;

internal static class FigureGraphPdfService
{
    /// <summary>
    /// Draws a grid of the Figure's assigned Analysis heatmaps, one small heatmap per cell, and writes
    /// it to the project's Output folder as "&lt;figureName&gt;.pdf". Cells with no assigned graph are
    /// left blank. Returns the written file path, or null if it could not be written.
    /// </summary>
    public static string? GeneratePdf(string projectFolder, string figureName, CreateFigureGraphResponse graph) =>
        GeneratePdf(LocalFileSystem.Instance, projectFolder, figureName, graph);

    internal static string? GeneratePdf(
        IFileSystem fileSystem, string? projectFolder, string figureName, CreateFigureGraphResponse graph)
    {
        if (string.IsNullOrWhiteSpace(projectFolder))
        {
            return null;
        }

        if (graph.Figure is null)
        {
            return null;
        }

        var figure = graph.Figure;

        var outputFolder = Path.Combine(projectFolder, ProjectFolderService.ProjectOutputFolderName);
        fileSystem.CreateDirectory(outputFolder);
        var outputFile = Path.Combine(outputFolder, figureName + ".pdf");

        using var stream = fileSystem.OpenWrite(outputFile);
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

        var cellOuterWidth = graphSize / figure.ColCount;
        var cellOuterHeight = graphSize / figure.RowCount;

        using var canvas = document.BeginPage(pageWidth, pageHeight);
        canvas.Clear(SKColors.White);

        using var borderPaint = new SKPaint
        {
            Color = SKColors.Gray,
            StrokeWidth = 1,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };

        var graphIndex = 0;
        for (var index = 0; index < figure.CellHasGraph.Length; index++)
        {
            var row = index / figure.ColCount;
            var col = index % figure.ColCount;
            var outerLeft = gridLeft + col * cellOuterWidth;
            var outerTop = gridTop + row * cellOuterHeight;
            var outerRect = new SKRect(outerLeft, outerTop, outerLeft + cellOuterWidth, outerTop + cellOuterHeight);

            if (figure.CellHasGraph[index])
            {
                HeatmapDrawing.Draw(canvas, outerRect, figure.Graphs[graphIndex]);
                graphIndex++;
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
