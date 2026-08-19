using System.Collections.Immutable;
using DataStructures;
using Messages;
using TaskManager;
using Path = System.IO.Path;

namespace OutMapper.Tests;

/// <summary>
/// Not part of the regression suite - an on-demand generator for a representative sample figure PDF, so
/// its appearance can be inspected directly (e.g. by Claude's Read tool) while the layout is still evolving.
/// Run with: dotnet test --filter SampleFigurePdfGenerator
/// </summary>
public class SampleFigurePdfGenerator
{
    [Fact(Skip = "On-demand visual-inspection helper, not a regression test.")]
    public void GenerateSampleFigurePdf()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "OutMapperSamplePdf");

        GraphDrawData Graph(string colorA, string colorB) => GraphDrawData.Create(
            channelAName: "Heart rate",
            channelBName: "Blood pressure",
            channelABinEdges: ImmutableArray.Create(0.0, 50.0, 100.0),
            channelBBinEdges: ImmutableArray.Create(0.0, 50.0, 100.0),
            cellColorsRowMajor: ImmutableArray.Create(colorA, colorB, colorB, colorA),
            drawAxisTickLabels: true,
            drawAxisTitles: true).Should().BeOfType<Success<GraphDrawData>>().Subject.Value;

        // 2x2 grid, top-right cell left empty, to exercise spacing, blank-cell borders, and label order.
        var figure = FigureDrawData.Create(
            rowCount: 2,
            colCount: 2,
            cellHasGraph: new[] { true, false, true, true },
            graphs: new[] { Graph("#2166AC", "#67A9CF"), Graph("#D1E5F0", "#FDDBC7"), Graph("#EF8A62", "#B2182B") },
            labelStyle: FigureLabelStyle.Uppercase).Should().BeOfType<Success<FigureDrawData>>().Subject.Value;

        var response = new CreateFigureGraphResponse(
            "SampleProject", "SampleFigure", Success: true, ErrorMessage: null, figure);

        var outputFile = FigureGraphPdfService.GeneratePdf(
            LocalFileSystem.Instance, outputDirectory, "SampleFigure", response);

        outputFile.Should().NotBeNull();
    }
}
