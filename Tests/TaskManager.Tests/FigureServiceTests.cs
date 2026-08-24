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
    public void CreateGraph_leaves_an_unassigned_cell_without_a_graph()
    {
        var fileSystem = new InMemoryFileSystem();
        fileSystem.CreateDirectory(ProjectFolder);

        var response = FigureService.CreateGraph(
            fileSystem, ProjectFolder, "MyFigure", rowCount: 1, colCount: 1, ImmutableArray.Create((string?)null));

        response.Success.Should().BeTrue();
        response.Figure!.CellHasGraph.Single().Should().BeFalse();
        response.Figure.Graphs.Should().BeEmpty();
    }

    [Fact]
    public void CreateGraph_fails_when_the_assigned_analysis_has_no_persisted_graph()
    {
        var fileSystem = new InMemoryFileSystem();
        fileSystem.CreateDirectory(ProjectFolder);

        var response = FigureService.CreateGraph(
            fileSystem, ProjectFolder, "MyFigure", rowCount: 1, colCount: 1, ImmutableArray.Create((string?)"MyAnalysis"));

        response.Success.Should().BeFalse();
        response.Figure.Should().BeNull();
        response.ErrorMessage.Should().NotBeNull();
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
        response.Figure!.CellHasGraph.Single().Should().BeTrue();
        var graph = response.Figure.Graphs.Single();
        graph.DrawAxisTickLabels.Should().BeTrue();
        graph.DrawAxisTitles.Should().BeTrue();

        response.PdfOutputPath.Should().Be(
            Path.Combine(ProjectFolder, "OutMapper_ProjectOutput", "MyFigure.pdf"));
        fileSystem.FileExists(response.PdfOutputPath!).Should().BeTrue();
        var pdfBytes = fileSystem.ReadAllBytes(response.PdfOutputPath!);
        System.Text.Encoding.ASCII.GetString(pdfBytes, 0, 4).Should().Be("%PDF");
    }

    [Fact]
    public void CreateGraph_persists_the_label_style_and_carries_it_into_the_returned_figure()
    {
        var fileSystem = new InMemoryFileSystem();
        fileSystem.CreateDirectory(ProjectFolder);

        var response = FigureService.CreateGraph(
            fileSystem, ProjectFolder, "MyFigure", rowCount: 1, colCount: 1, ImmutableArray.Create((string?)null),
            FigureLabelStyle.Uppercase);

        response.Success.Should().BeTrue();
        response.Figure!.LabelStyle.Should().Be(FigureLabelStyle.Uppercase);

        var layout = FigureService.ReadLayout(fileSystem, ProjectFolder, "MyFigure");
        layout.LabelStyle.Should().Be(FigureLabelStyle.Uppercase);
    }

    [Fact]
    public void BuildDrawData_leaves_an_unassigned_cell_without_a_graph()
    {
        var fileSystem = new InMemoryFileSystem();
        fileSystem.CreateDirectory(ProjectFolder);

        var result = FigureService.BuildDrawData(
            fileSystem, ProjectFolder, rowCount: 1, colCount: 1, ImmutableArray.Create((string?)null),
            FigureLabelStyle.None);

        var figure = result.Should().BeOfType<Success<FigureDrawData>>().Subject.Value;
        figure.CellHasGraph.Single().Should().BeFalse();
        figure.Graphs.Should().BeEmpty();
    }

    [Fact]
    public void BuildDrawData_fails_when_the_assigned_analysis_has_no_persisted_graph()
    {
        var fileSystem = new InMemoryFileSystem();
        fileSystem.CreateDirectory(ProjectFolder);

        var result = FigureService.BuildDrawData(
            fileSystem, ProjectFolder, rowCount: 1, colCount: 1, ImmutableArray.Create((string?)"MyAnalysis"),
            FigureLabelStyle.None);

        result.Should().BeOfType<Failure<FigureDrawData>>();
    }

    [Fact]
    public void BuildDrawData_does_not_persist_a_figure_config()
    {
        var fileSystem = new InMemoryFileSystem();
        fileSystem.CreateDirectory(ProjectFolder);

        FigureService.BuildDrawData(
            fileSystem, ProjectFolder, rowCount: 1, colCount: 1, ImmutableArray.Create((string?)null),
            FigureLabelStyle.Uppercase);

        var layout = FigureService.ReadLayout(fileSystem, ProjectFolder, "MyFigure");
        layout.LayoutExists.Should().BeFalse();
    }

    [Fact]
    public void SaveSize_preserves_the_previously_saved_label_style()
    {
        var fileSystem = new InMemoryFileSystem();
        fileSystem.CreateDirectory(ProjectFolder);

        FigureService.CreateGraph(
            fileSystem, ProjectFolder, "MyFigure", rowCount: 1, colCount: 1, ImmutableArray.Create((string?)null),
            FigureLabelStyle.Lowercase);

        var response = FigureService.SaveSize(fileSystem, ProjectFolder, "MyFigure", rowCount: 2, colCount: 2);

        response.Success.Should().BeTrue();
        response.LabelStyle.Should().Be(FigureLabelStyle.Lowercase);

        var layout = FigureService.ReadLayout(fileSystem, ProjectFolder, "MyFigure");
        layout.LabelStyle.Should().Be(FigureLabelStyle.Lowercase);
    }
}
