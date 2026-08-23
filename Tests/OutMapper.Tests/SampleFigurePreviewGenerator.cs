using System.Collections.Immutable;
using DataStructures;
using TaskManager;
using TestSupport;

namespace OutMapper.Tests;

/// <summary>
/// Generates a representative sample figure preview PNG, so its appearance can be inspected directly
/// (e.g. by Claude's Read tool) while the preview rendering is still evolving.
/// </summary>
public class SampleFigurePreviewGenerator
{
    [Fact]
    public void GenerateSampleFigurePreviewPng()
    {
        var outputDirectory = SampleOutputDirectory.For(nameof(SampleFigurePreviewGenerator));

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

        var pngBytes = FigurePreviewRenderer.RenderPng(figure, maxDimensionPx: 900);

        pngBytes.Should().NotBeNull();

        LocalFileSystem.Instance.CreateDirectory(outputDirectory);
        LocalFileSystem.Instance.WriteAllBytes(
            System.IO.Path.Combine(outputDirectory, "SampleFigurePreview.png"), pngBytes!);
    }
}
