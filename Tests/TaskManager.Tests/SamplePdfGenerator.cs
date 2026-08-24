using System.Collections.Immutable;
using DataStructures;
using TestSupport;

namespace TaskManager.Tests;

/// <summary>
/// Generates a representative sample analysis PDF, so its appearance can be inspected directly (e.g. by
/// Claude's Read tool) while the layout is still evolving.
/// </summary>
public class SamplePdfGenerator
{
    [Fact]
    public void GenerateSampleAnalysisPdf()
    {
        var outputDirectory = SampleOutputDirectory.For(nameof(SamplePdfGenerator));
        var sampleGraph = GraphDrawData.Create(
            channelAName: "Heart rate",
            channelBName: "Blood pressure",
            channelABinEdges: ImmutableArray.Create(0.0, 25.0, 50.0, 75.0, 100.0),
            channelBBinEdges: ImmutableArray.Create(0.0, 50.0, 100.0),
            cellColorsRowMajor: ImmutableArray.Create(
                "#2166AC", "#67A9CF", "#D1E5F0", "#FDDBC7", "#EF8A62", "#B2182B", "#67A9CF", "#2166AC"),
            drawAxisTickLabels: true,
            drawAxisTitles: true).Should().BeOfType<Success<GraphDrawData>>().Subject.Value;

        var outputFile = AnalysisGraphPdfService.GeneratePdf(
            LocalFileSystem.Instance, outputDirectory, "SampleAnalysis", sampleGraph);

        outputFile.Should().NotBeNull();
    }
}
