using SkiaSharp;
using System.IO;
using Messages;
using TaskManager;
using Path = System.IO.Path;

namespace OutMapper;

internal static class AnalysisGraphPdfService
{
    /// <summary>
    /// Draws the association grid from a <see cref="GenerateAnalysisGraphResponse"/> as an N x M
    /// colored heatmap and writes it to the project's Output folder as "&lt;analysisName&gt;.pdf".
    /// Returns the written file path, or null if it could not be written.
    /// </summary>
    public static string? GeneratePdf(string projectName, string analysisName, GenerateAnalysisGraphResponse graph) =>
        GeneratePdf(LocalFileSystem.Instance, SettingsWorkspaceContent.LoadWorkspaceFolderPath(), projectName, analysisName, graph);

    internal static string? GeneratePdf(
        IFileSystem fileSystem, string? workspaceFolder, string projectName, string analysisName, GenerateAnalysisGraphResponse graph)
    {
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
        fileSystem.CreateDirectory(outputFolder);
        var outputFile = Path.Combine(outputFolder, analysisName + ".pdf");

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
        const float axisLeft = margin + 40f;
        const float axisBottom = pageHeight - margin - 24f;
        const float axisTop = axisBottom - graphSize;
        const float axisRight = axisLeft + graphSize;

        using var canvas = document.BeginPage(pageWidth, pageHeight);
        canvas.Clear(SKColors.White);

        var heatmapData = new HeatmapData(
            graph.ChannelABinEdges, graph.ChannelBBinEdges, graph.CellColorsRowMajor, graph.ChannelAName, graph.ChannelBName);
        HeatmapDrawing.Draw(
            canvas, new SKRect(axisLeft, axisTop, axisRight, axisBottom), heatmapData,
            drawAxisTickLabels: true, drawAxisTitles: true);

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
}
