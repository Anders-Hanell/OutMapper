using System.Collections.Immutable;
using DataStructures;
using TestSupport;
using Path = System.IO.Path;

namespace TaskManager.Tests;

public class AnalysisGraphPdfServiceTests
{
    private const string ProjectOutputFolderName = "OutMapper_ProjectOutput";

    private static GraphDrawData SampleGraph() =>
        GraphDrawData.Create(
            channelAName: "A",
            channelBName: "B",
            channelABinEdges: ImmutableArray.Create(0.0, 1.0),
            channelBBinEdges: ImmutableArray.Create(0.0, 1.0),
            cellColorsRowMajor: ImmutableArray.Create("#FF0000"),
            drawAxisTickLabels: true,
            drawAxisTitles: true).Should().BeOfType<Success<GraphDrawData>>().Subject.Value;

    [Fact]
    public void GeneratePdf_writes_a_pdf_file_to_the_project_output_folder()
    {
        var fileSystem = new InMemoryFileSystem();
        const string projectFolder = "/projects/MyProject";
        const string analysisName = "MyAnalysis";

        var outputFile = AnalysisGraphPdfService.GeneratePdf(fileSystem, projectFolder, analysisName, SampleGraph());

        outputFile.Should().Be(Path.Combine(projectFolder, ProjectOutputFolderName, analysisName + ".pdf"));
        fileSystem.FileExists(outputFile!).Should().BeTrue();

        var bytes = fileSystem.ReadAllBytes(outputFile!);
        bytes.Length.Should().BeGreaterThan(0);
        System.Text.Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
    }

    [Fact]
    public void GeneratePdf_returns_null_without_a_project_folder()
    {
        var fileSystem = new InMemoryFileSystem();

        var outputFile = AnalysisGraphPdfService.GeneratePdf(fileSystem, projectFolder: null, "MyAnalysis", SampleGraph());

        outputFile.Should().BeNull();
    }
}
