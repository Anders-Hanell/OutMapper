using SkiaSharp;
using System.IO;
using System.Text;
using DataStructures;
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
        const float cellGap = 16f;
        const float labelHeight = 16f;

        var cellOuterWidth = (graphSize - (figure.ColCount - 1) * cellGap) / figure.ColCount;
        var cellOuterHeight = (graphSize - (figure.RowCount - 1) * cellGap) / figure.RowCount;

        using var canvas = document.BeginPage(pageWidth, pageHeight);
        canvas.Clear(SKColors.White);

        using var borderPaint = new SKPaint
        {
            Color = SKColors.Gray,
            StrokeWidth = 1,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };

        using var labelPaint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true
        };

        using var labelFont = new SKFont { Size = 14, Embolden = true };

        var hasLabels = figure.LabelStyle != FigureLabelStyle.None;
        var graphIndex = 0;
        for (var index = 0; index < figure.CellHasGraph.Length; index++)
        {
            var row = index / figure.ColCount;
            var col = index % figure.ColCount;
            var outerLeft = gridLeft + col * (cellOuterWidth + cellGap);
            var outerTop = gridTop + row * (cellOuterHeight + cellGap);
            var outerRect = new SKRect(outerLeft, outerTop, outerLeft + cellOuterWidth, outerTop + cellOuterHeight);

            if (figure.CellHasGraph[index])
            {
                var cellGraph = figure.Graphs[graphIndex];

                // HeatmapDrawing draws tick labels and axis titles outside the rect it's given, so each
                // cell must shrink its own rect to keep that chrome from bleeding into the next cell.
                var (reservedLeft, reservedBottom) = HeatmapLayoutService.ComputeReservedMargins(cellGraph);

                var heatmapRect = new SKRect(
                    outerRect.Left + (float)reservedLeft,
                    hasLabels ? outerRect.Top + labelHeight : outerRect.Top,
                    outerRect.Right,
                    outerRect.Bottom - (float)reservedBottom);

                HeatmapDrawing.Draw(canvas, heatmapRect, cellGraph);

                if (hasLabels)
                {
                    // Top-right, not top-left: the rotated y-axis title (when present) occupies the
                    // cell's left edge and can run taller than the cell, so top-left risks a collision.
                    var label = GetLetterLabel(graphIndex, uppercase: figure.LabelStyle == FigureLabelStyle.Uppercase);
                    canvas.DrawText(label, outerRect.Right, outerRect.Top + labelHeight - 2f, SKTextAlign.Right, labelFont, labelPaint);
                }

                graphIndex++;
            }
            else
            {
                canvas.DrawRect(outerRect, borderPaint);
            }
        }

        document.EndPage();
        document.Close();

        return outputFile;
    }

    /// <summary>
    /// Spreadsheet-style column label for a zero-based index: 0 -> "A", 25 -> "Z", 26 -> "AA", and so on,
    /// so a figure with more than 26 labeled graphs still gets distinct, alphabetically ordered labels.
    /// </summary>
    private static string GetLetterLabel(int index, bool uppercase)
    {
        var n = index + 1;
        var builder = new StringBuilder();
        while (n > 0)
        {
            n--;
            builder.Insert(0, (char)('A' + n % 26));
            n /= 26;
        }

        var label = builder.ToString();
        return uppercase ? label : label.ToLowerInvariant();
    }
}
