using DataStructures;
using Path = System.IO.Path;

namespace TaskManager;

/// <summary>
/// Persists a Figure's PDF to the project's Output folder as "&lt;figureName&gt;.pdf". The PDF bytes
/// themselves are computed by <see cref="FigurePdfGenerator"/>, which knows nothing about files or
/// paths; this class only resolves the output path and writes what it's given. Returns the written
/// file path, or null if it could not be generated or written.
/// </summary>
internal static class FigureGraphPdfService
{
    private const string ProjectOutputFolderName = "OutMapper_ProjectOutput";

    internal static string? GeneratePdf(
        IFileSystem fileSystem, string? projectFolder, string figureName, FigureDrawData? figure)
    {
        if (string.IsNullOrWhiteSpace(projectFolder) || figure is null)
        {
            return null;
        }

        var pdfBytes = FigurePdfGenerator.Generate(figure);
        if (pdfBytes is null)
        {
            return null;
        }

        var outputFolder = Path.Combine(projectFolder, ProjectOutputFolderName);
        fileSystem.CreateDirectory(outputFolder);
        var outputFile = Path.Combine(outputFolder, figureName + ".pdf");
        fileSystem.WriteAllBytes(outputFile, pdfBytes);

        return outputFile;
    }
}
