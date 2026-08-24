using System.Collections.Immutable;
using DataStructures;

namespace TaskManager.Tests;

public class FigurePdfGeneratorTests
{
    [Fact]
    public void Generate_succeeds_when_the_figure_has_no_labels()
    {
        var graph = GraphDrawData.Create(
            channelAName: "Heart rate",
            channelBName: "Blood pressure",
            channelABinEdges: ImmutableArray.Create(0.0, 50.0),
            channelBBinEdges: ImmutableArray.Create(0.0, 50.0),
            cellColorsRowMajor: ImmutableArray.Create("#2166AC"),
            drawAxisTickLabels: false,
            drawAxisTitles: false).Should().BeOfType<Success<GraphDrawData>>().Subject.Value;

        var figure = FigureDrawData.Create(
            rowCount: 1, colCount: 1, cellHasGraph: [true], graphs: [graph],
            labelStyle: FigureLabelStyle.None).Should().BeOfType<Success<FigureDrawData>>().Subject.Value;

        var pdfBytes = FigurePdfGenerator.Generate(figure);

        pdfBytes.Should().NotBeNull();
    }
}
