using SkiaSharp;
using DataStructures;
using Path = System.IO.Path;

namespace TaskManager;

/// <summary>
/// Draws an Analysis's association grid as an N x M colored heatmap and writes it to the project's
/// Output folder as "&lt;analysisName&gt;.pdf". Returns the written file path, or null if it could not
/// be written.
/// </summary>
internal static class AnalysisGraphPdfService
{
    private const string ProjectOutputFolderName = "OutMapper_ProjectOutput";

    internal static string? GeneratePdf(
        IFileSystem fileSystem, string? projectFolder, string analysisName, GraphDrawData? graph)
    {
        if (string.IsNullOrWhiteSpace(projectFolder) || graph is null)
        {
            return null;
        }

        var outputFolder = Path.Combine(projectFolder, ProjectOutputFolderName);
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

        HeatmapDrawing.Draw(canvas, new SKRect(axisLeft, axisTop, axisRight, axisBottom), graph);

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
