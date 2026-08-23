using System.IO;
using Messages;
using TaskManager;
using Path = System.IO.Path;

namespace OutMapper;

/// <summary>
/// Persists a Figure's PDF to the project's Output folder as "&lt;figureName&gt;.pdf". The PDF bytes
/// themselves are computed by <see cref="FigurePdfGenerator"/>, which knows nothing about files or
/// paths; this class only resolves the output path and writes what it's given. Returns the written
/// file path, or null if it could not be generated or written.
/// </summary>
internal static class FigureGraphPdfService
{
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

        var pdfBytes = FigurePdfGenerator.Generate(graph.Figure);
        if (pdfBytes is null)
        {
            return null;
        }

        var outputFolder = Path.Combine(projectFolder, ProjectFolderService.ProjectOutputFolderName);
        fileSystem.CreateDirectory(outputFolder);
        var outputFile = Path.Combine(outputFolder, figureName + ".pdf");
        fileSystem.WriteAllBytes(outputFile, pdfBytes);

        return outputFile;
    }
}
