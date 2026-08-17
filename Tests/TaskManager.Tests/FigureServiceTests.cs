using System.Collections.Immutable;
using DataStructures;
using TestSupport;
using Path = System.IO.Path;

namespace TaskManager.Tests;

public class FigureServiceTests
{
    private const string ProjectFolder = "/projects/MyProject";

    private static void WritePersistedGraph(
        IFileSystem fileSystem, string analysisName, bool drawAxisTickLabels, bool drawAxisTitles)
    {
        var graph = GraphDrawData.Create(
            channelAName: "ICP",
            channelBName: "PRx",
            channelABinEdges: ImmutableArray.Create(0.0, 10.0),
            channelBBinEdges: ImmutableArray.Create(-1.0, 1.0),
            cellColorsRowMajor: ImmutableArray.Create("#FF0000"),
            drawAxisTickLabels,
            drawAxisTitles).Should().BeOfType<Success<GraphDrawData>>().Subject.Value;

        fileSystem.WriteAllBytes(
            Path.Combine(ProjectFolder, "OutMapper_InternalFiles", "Analyses", analysisName, "graph-data.json"),
            graph.ToByteArray().ToArray());
    }

    [Fact]
    public void CreateGraph_leaves_an_unassigned_cell_without_a_graph_or_an_error()
    {
        var fileSystem = new InMemoryFileSystem();
        fileSystem.CreateDirectory(ProjectFolder);

        var response = FigureService.CreateGraph(
            fileSystem, ProjectFolder, "MyFigure", rowCount: 1, colCount: 1, ImmutableArray.Create((string?)null));

        response.Success.Should().BeTrue();
        var cell = response.Figure!.Cells.Single();
        cell.HasGraph.Should().BeFalse();
        cell.ErrorMessage.Should().BeNull();
        cell.AnalysisName.Should().BeNull();
    }

    [Fact]
    public void CreateGraph_reports_an_error_when_the_assigned_analysis_has_no_persisted_graph()
    {
        var fileSystem = new InMemoryFileSystem();
        fileSystem.CreateDirectory(ProjectFolder);

        var response = FigureService.CreateGraph(
            fileSystem, ProjectFolder, "MyFigure", rowCount: 1, colCount: 1, ImmutableArray.Create((string?)"MyAnalysis"));

        response.Success.Should().BeTrue();
        var cell = response.Figure!.Cells.Single();
        cell.HasGraph.Should().BeFalse();
        cell.AnalysisName.Should().Be("MyAnalysis");
        cell.ErrorMessage.Should().NotBeNull();
    }

    [Fact]
    public void CreateGraph_attaches_the_persisted_graph_with_its_own_chrome_flags_unchanged()
    {
        var fileSystem = new InMemoryFileSystem();
        fileSystem.CreateDirectory(ProjectFolder);
        WritePersistedGraph(fileSystem, "MyAnalysis", drawAxisTickLabels: true, drawAxisTitles: true);

        var response = FigureService.CreateGraph(
            fileSystem, ProjectFolder, "MyFigure", rowCount: 1, colCount: 1, ImmutableArray.Create((string?)"MyAnalysis"));

        response.Success.Should().BeTrue();
        var cell = response.Figure!.Cells.Single();
        cell.HasGraph.Should().BeTrue();
        cell.ErrorMessage.Should().BeNull();
        cell.Graph!.DrawAxisTickLabels.Should().BeTrue();
        cell.Graph.DrawAxisTitles.Should().BeTrue();
    }
}
