using System.Collections.Immutable;

namespace DataStructures.Tests;

public class FigureCellDrawDataTests
{
    private static GraphDrawData Graph()
    {
        return GraphDrawData.Create(
            "ICP", "PRx", ImmutableArray.Create(0.0, 10.0), ImmutableArray.Create(-1.0, 1.0),
            ImmutableArray.Create("#FF0000"), drawAxisTickLabels: true, drawAxisTitles: true)
            .Should().BeOfType<Success<GraphDrawData>>().Subject.Value;
    }

    [Fact]
    public void Create_succeeds_for_an_empty_unassigned_cell()
    {
        var cell = FigureCellDrawData.Create(0, 0, analysisName: null, graph: null, errorMessage: null)
            .Should().BeOfType<Success<FigureCellDrawData>>().Subject.Value;

        cell.HasGraph.Should().BeFalse();
    }

    [Fact]
    public void Create_succeeds_for_a_cell_with_a_graph()
    {
        var cell = FigureCellDrawData.Create(0, 0, "MyAnalysis", Graph(), errorMessage: null)
            .Should().BeOfType<Success<FigureCellDrawData>>().Subject.Value;

        cell.HasGraph.Should().BeTrue();
    }

    [Fact]
    public void Create_succeeds_for_a_cell_with_an_error()
    {
        var cell = FigureCellDrawData.Create(0, 0, "MyAnalysis", graph: null, "Something went wrong.")
            .Should().BeOfType<Success<FigureCellDrawData>>().Subject.Value;

        cell.HasGraph.Should().BeFalse();
        cell.ErrorMessage.Should().Be("Something went wrong.");
    }

    [Fact]
    public void Create_fails_when_the_row_is_negative()
    {
        var result = FigureCellDrawData.Create(-1, 0, analysisName: null, graph: null, errorMessage: null);

        result.Should().BeOfType<Failure<FigureCellDrawData>>();
    }

    [Fact]
    public void Create_fails_when_the_column_is_negative()
    {
        var result = FigureCellDrawData.Create(0, -1, analysisName: null, graph: null, errorMessage: null);

        result.Should().BeOfType<Failure<FigureCellDrawData>>();
    }

    [Fact]
    public void Create_fails_when_both_a_graph_and_an_error_message_are_given()
    {
        var result = FigureCellDrawData.Create(0, 0, "MyAnalysis", Graph(), "Something went wrong.");

        result.Should().BeOfType<Failure<FigureCellDrawData>>();
    }

    [Fact]
    public void Create_fails_when_a_graph_is_given_without_an_analysis_name()
    {
        var result = FigureCellDrawData.Create(0, 0, analysisName: null, Graph(), errorMessage: null);

        result.Should().BeOfType<Failure<FigureCellDrawData>>();
    }

    [Fact]
    public void ToByteArray_and_FromByteArray_round_trips_an_empty_cell()
    {
        var original = FigureCellDrawData.Create(1, 2, analysisName: null, graph: null, errorMessage: null)
            .Should().BeOfType<Success<FigureCellDrawData>>().Subject.Value;

        var roundTripped = FigureCellDrawData.FromByteArray(original.ToByteArray())
            .Should().BeOfType<Success<FigureCellDrawData>>().Subject.Value;

        roundTripped.Row.Should().Be(1);
        roundTripped.Col.Should().Be(2);
        roundTripped.HasGraph.Should().BeFalse();
    }

    [Fact]
    public void ToByteArray_and_FromByteArray_round_trips_a_cell_with_a_graph()
    {
        var original = FigureCellDrawData.Create(1, 2, "MyAnalysis", Graph(), errorMessage: null)
            .Should().BeOfType<Success<FigureCellDrawData>>().Subject.Value;

        var roundTripped = FigureCellDrawData.FromByteArray(original.ToByteArray())
            .Should().BeOfType<Success<FigureCellDrawData>>().Subject.Value;

        roundTripped.AnalysisName.Should().Be("MyAnalysis");
        roundTripped.Graph.Should().NotBeNull();
        roundTripped.Graph!.ChannelAName.Should().Be(original.Graph!.ChannelAName);
        roundTripped.Graph.DrawAxisTickLabels.Should().Be(original.Graph.DrawAxisTickLabels);
    }

    [Fact]
    public void ToByteArray_and_FromByteArray_round_trips_a_cell_with_an_error()
    {
        var original = FigureCellDrawData.Create(1, 2, "MyAnalysis", graph: null, "Something went wrong.")
            .Should().BeOfType<Success<FigureCellDrawData>>().Subject.Value;

        var roundTripped = FigureCellDrawData.FromByteArray(original.ToByteArray())
            .Should().BeOfType<Success<FigureCellDrawData>>().Subject.Value;

        roundTripped.ErrorMessage.Should().Be("Something went wrong.");
        roundTripped.HasGraph.Should().BeFalse();
    }
}
