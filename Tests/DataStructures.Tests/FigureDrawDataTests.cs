using System.Collections.Immutable;

namespace DataStructures.Tests;

public class FigureDrawDataTests
{
    private static FigureCellDrawData EmptyCell(int row, int col)
    {
        return FigureCellDrawData.Create(row, col, analysisName: null, graph: null, errorMessage: null)
            .Should().BeOfType<Success<FigureCellDrawData>>().Subject.Value;
    }

    private static FigureCellDrawData GraphCell(int row, int col)
    {
        var graph = GraphDrawData.Create(
            "ICP", "PRx", ImmutableArray.Create(0.0, 10.0), ImmutableArray.Create(-1.0, 1.0),
            ImmutableArray.Create("#FF0000"), drawAxisTickLabels: true, drawAxisTitles: false)
            .Should().BeOfType<Success<GraphDrawData>>().Subject.Value;

        return FigureCellDrawData.Create(row, col, "MyAnalysis", graph, errorMessage: null)
            .Should().BeOfType<Success<FigureCellDrawData>>().Subject.Value;
    }

    [Fact]
    public void Create_succeeds_for_a_full_grid_of_cells()
    {
        var cells = new[] { EmptyCell(0, 0), GraphCell(0, 1) };

        var figure = FigureDrawData.Create(1, 2, cells).Should().BeOfType<Success<FigureDrawData>>().Subject.Value;

        figure.Cells.Should().HaveCount(2);
    }

    [Fact]
    public void Create_fails_when_row_count_is_not_positive()
    {
        var result = FigureDrawData.Create(0, 1, new[] { EmptyCell(0, 0) });

        result.Should().BeOfType<Failure<FigureDrawData>>();
    }

    [Fact]
    public void Create_fails_when_column_count_is_not_positive()
    {
        var result = FigureDrawData.Create(1, 0, new[] { EmptyCell(0, 0) });

        result.Should().BeOfType<Failure<FigureDrawData>>();
    }

    [Fact]
    public void Create_fails_when_the_cell_count_does_not_match_the_grid_size()
    {
        var result = FigureDrawData.Create(1, 2, new[] { EmptyCell(0, 0) });

        result.Should().BeOfType<Failure<FigureDrawData>>();
    }

    [Fact]
    public void Create_fails_when_a_cell_is_outside_the_grid()
    {
        var result = FigureDrawData.Create(1, 1, new[] { EmptyCell(0, 5) });

        result.Should().BeOfType<Failure<FigureDrawData>>();
    }

    [Fact]
    public void Create_fails_when_two_cells_share_the_same_position()
    {
        var result = FigureDrawData.Create(1, 2, new[] { EmptyCell(0, 0), EmptyCell(0, 0) });

        result.Should().BeOfType<Failure<FigureDrawData>>();
    }

    [Fact]
    public void ToByteArray_and_FromByteArray_round_trips_a_mix_of_empty_and_graph_cells()
    {
        var original = FigureDrawData.Create(1, 2, new[] { EmptyCell(0, 0), GraphCell(0, 1) })
            .Should().BeOfType<Success<FigureDrawData>>().Subject.Value;

        var roundTripped = FigureDrawData.FromByteArray(original.ToByteArray())
            .Should().BeOfType<Success<FigureDrawData>>().Subject.Value;

        roundTripped.RowCount.Should().Be(1);
        roundTripped.ColCount.Should().Be(2);
        roundTripped.Cells.Should().HaveCount(2);
        roundTripped.Cells[0].HasGraph.Should().BeFalse();
        roundTripped.Cells[1].HasGraph.Should().BeTrue();
        roundTripped.Cells[1].Graph!.DrawAxisTickLabels.Should().BeTrue();
        roundTripped.Cells[1].Graph!.DrawAxisTitles.Should().BeFalse();
    }
}
