using System.Collections.Immutable;

namespace DataStructures.Tests;

public class FigureDrawDataTests
{
    private static GraphDrawData Graph()
    {
        return GraphDrawData.Create(
            "ICP", "PRx", ImmutableArray.Create(0.0, 10.0), ImmutableArray.Create(-1.0, 1.0),
            ImmutableArray.Create("#FF0000"), drawAxisTickLabels: true, drawAxisTitles: false)
            .Should().BeOfType<Success<GraphDrawData>>().Subject.Value;
    }

    [Fact]
    public void Create_succeeds_for_a_full_grid_of_cells()
    {
        var figure = FigureDrawData.Create(1, 2, new[] { false, true }, new[] { Graph() })
            .Should().BeOfType<Success<FigureDrawData>>().Subject.Value;

        figure.CellHasGraph.Should().Equal(false, true);
        figure.Graphs.Should().HaveCount(1);
    }

    [Fact]
    public void Create_fails_when_row_count_is_not_positive()
    {
        var result = FigureDrawData.Create(0, 1, new[] { false }, Array.Empty<GraphDrawData>());

        result.Should().BeOfType<Failure<FigureDrawData>>();
    }

    [Fact]
    public void Create_fails_when_column_count_is_not_positive()
    {
        var result = FigureDrawData.Create(1, 0, new[] { false }, Array.Empty<GraphDrawData>());

        result.Should().BeOfType<Failure<FigureDrawData>>();
    }

    [Fact]
    public void Create_fails_when_the_cell_flag_count_does_not_match_the_grid_size()
    {
        var result = FigureDrawData.Create(1, 2, new[] { false }, Array.Empty<GraphDrawData>());

        result.Should().BeOfType<Failure<FigureDrawData>>();
    }

    [Fact]
    public void Create_fails_when_the_graph_count_does_not_match_the_number_of_flagged_cells()
    {
        var result = FigureDrawData.Create(1, 2, new[] { false, true }, Array.Empty<GraphDrawData>());

        result.Should().BeOfType<Failure<FigureDrawData>>();
    }

    [Fact]
    public void ToByteArray_and_FromByteArray_round_trips_a_mix_of_empty_and_graph_cells()
    {
        var original = FigureDrawData.Create(1, 2, new[] { false, true }, new[] { Graph() })
            .Should().BeOfType<Success<FigureDrawData>>().Subject.Value;

        var roundTripped = FigureDrawData.FromByteArray(original.ToByteArray())
            .Should().BeOfType<Success<FigureDrawData>>().Subject.Value;

        roundTripped.RowCount.Should().Be(1);
        roundTripped.ColCount.Should().Be(2);
        roundTripped.CellHasGraph.Should().Equal(false, true);
        roundTripped.Graphs.Should().HaveCount(1);
        roundTripped.Graphs[0].DrawAxisTickLabels.Should().BeTrue();
        roundTripped.Graphs[0].DrawAxisTitles.Should().BeFalse();
    }
}
