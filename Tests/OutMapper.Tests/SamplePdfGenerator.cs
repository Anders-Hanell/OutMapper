using System.Collections.Immutable;
using DataStructures;
using Messages;
using TaskManager;
using Path = System.IO.Path;

namespace OutMapper.Tests;

/// <summary>
/// Not part of the regression suite - an on-demand generator for a representative sample PDF, so its
/// appearance can be inspected directly (e.g. by Claude's Read tool) while the layout is still evolving.
/// Run with: dotnet test --filter SamplePdfGenerator
/// </summary>
public class SamplePdfGenerator
{
    [Fact(Skip = "On-demand visual-inspection helper, not a regression test.")]
    public void GenerateSampleAnalysisPdf()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "OutMapperSamplePdf");
        var sampleGraph = GraphDrawData.Create(
            channelAName: "Heart rate",
            channelBName: "Blood pressure",
            channelABinEdges: ImmutableArray.Create(0.0, 25.0, 50.0, 75.0, 100.0),
            channelBBinEdges: ImmutableArray.Create(0.0, 50.0, 100.0),
            cellColorsRowMajor: ImmutableArray.Create(
                "#2166AC", "#67A9CF", "#D1E5F0", "#FDDBC7", "#EF8A62", "#B2182B", "#67A9CF", "#2166AC"),
            drawAxisTickLabels: true,
            drawAxisTitles: true).Should().BeOfType<Success<GraphDrawData>>().Subject.Value;

        var graph = new GenerateAnalysisGraphResponse(
            "SampleProject",
            "SampleAnalysis",
            Success: true,
            ErrorMessage: null,
            CohortName: "SampleCohort",
            TotalPatientCount: 42,
            MatchedPatientCount: 40,
            UnmatchedPatientCount: 2,
            AmbiguousPatientCount: 0,
            sampleGraph);

        var outputFile = AnalysisGraphPdfService.GeneratePdf(
            LocalFileSystem.Instance, outputDirectory, "SampleAnalysis", graph);

        outputFile.Should().NotBeNull();
    }
}
