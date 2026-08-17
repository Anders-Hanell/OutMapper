using System.Collections.Immutable;
using DataStructures;
using Messages;
using TestSupport;
using Path = System.IO.Path;

namespace OutMapper.Tests;

public class AnalysisGraphPdfServiceTests
{
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

        var graph = new GenerateAnalysisGraphResponse(
            projectFolder,
            analysisName,
            Success: true,
            ErrorMessage: null,
            CohortName: "MyCohort",
            TotalPatientCount: 10,
            MatchedPatientCount: 10,
            UnmatchedPatientCount: 0,
            AmbiguousPatientCount: 0,
            SampleGraph());

        var outputFile = AnalysisGraphPdfService.GeneratePdf(fileSystem, projectFolder, analysisName, graph);

        outputFile.Should().Be(Path.Combine(
            projectFolder, ProjectFolderService.ProjectOutputFolderName, analysisName + ".pdf"));
        fileSystem.FileExists(outputFile!).Should().BeTrue();

        var bytes = fileSystem.ReadAllBytes(outputFile!);
        bytes.Length.Should().BeGreaterThan(0);
        System.Text.Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
    }

    [Fact]
    public void GeneratePdf_returns_null_without_a_project_folder()
    {
        var fileSystem = new InMemoryFileSystem();

        var outputFile = AnalysisGraphPdfService.GeneratePdf(
            fileSystem, projectFolder: null, "MyAnalysis",
            new GenerateAnalysisGraphResponse(
                "MyProject", "MyAnalysis", Success: true, ErrorMessage: null, CohortName: "MyCohort",
                TotalPatientCount: 0, MatchedPatientCount: 0, UnmatchedPatientCount: 0, AmbiguousPatientCount: 0,
                SampleGraph()));

        outputFile.Should().BeNull();
    }
}
