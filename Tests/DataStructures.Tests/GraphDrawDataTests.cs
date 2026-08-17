using System.Collections.Immutable;

namespace DataStructures.Tests;

public class GraphDrawDataTests
{
    private static Result<GraphDrawData> CreateValid(
        ImmutableArray<double>? channelABinEdges = null, ImmutableArray<double>? channelBBinEdges = null,
        ImmutableArray<string>? cellColorsRowMajor = null)
    {
        return GraphDrawData.Create(
            "ICP",
            "PRx",
            channelABinEdges ?? ImmutableArray.Create(0.0, 5.0, 10.0),
            channelBBinEdges ?? ImmutableArray.Create(-1.0, 1.0),
            cellColorsRowMajor ?? ImmutableArray.Create("#FF0000", "#00FF00"),
            drawAxisTickLabels: true,
            drawAxisTitles: true);
    }

    [Fact]
    public void Create_succeeds_and_derives_row_and_column_counts()
    {
        var graph = CreateValid().Should().BeOfType<Success<GraphDrawData>>().Subject.Value;

        graph.ColCount.Should().Be(2);
        graph.RowCount.Should().Be(1);
    }

    [Fact]
    public void Create_fails_when_the_first_channel_name_is_blank()
    {
        var result = GraphDrawData.Create(
            "   ", "PRx", ImmutableArray.Create(0.0, 1.0), ImmutableArray.Create(0.0, 1.0),
            ImmutableArray.Create("#FF0000"), drawAxisTickLabels: false, drawAxisTitles: false);

        result.Should().BeOfType<Failure<GraphDrawData>>();
    }

    [Fact]
    public void Create_fails_when_the_second_channel_name_is_blank()
    {
        var result = GraphDrawData.Create(
            "ICP", "   ", ImmutableArray.Create(0.0, 1.0), ImmutableArray.Create(0.0, 1.0),
            ImmutableArray.Create("#FF0000"), drawAxisTickLabels: false, drawAxisTitles: false);

        result.Should().BeOfType<Failure<GraphDrawData>>();
    }

    [Fact]
    public void Create_fails_when_the_first_channels_bin_edges_have_fewer_than_two_values()
    {
        var result = CreateValid(channelABinEdges: ImmutableArray.Create(0.0));

        result.Should().BeOfType<Failure<GraphDrawData>>();
    }

    [Fact]
    public void Create_fails_when_the_second_channels_bin_edges_have_fewer_than_two_values()
    {
        var result = CreateValid(channelBBinEdges: ImmutableArray.Create(0.0));

        result.Should().BeOfType<Failure<GraphDrawData>>();
    }

    [Fact]
    public void Create_fails_when_the_first_channels_bin_edges_are_not_strictly_increasing()
    {
        var result = CreateValid(channelABinEdges: ImmutableArray.Create(0.0, 0.0, 10.0));

        result.Should().BeOfType<Failure<GraphDrawData>>();
    }

    [Fact]
    public void Create_fails_when_the_second_channels_bin_edges_are_not_strictly_increasing()
    {
        var result = CreateValid(channelBBinEdges: ImmutableArray.Create(1.0, -1.0));

        result.Should().BeOfType<Failure<GraphDrawData>>();
    }

    [Fact]
    public void Create_fails_when_the_cell_color_count_does_not_match_the_grid_size()
    {
        var result = CreateValid(cellColorsRowMajor: ImmutableArray.Create("#FF0000"));

        result.Should().BeOfType<Failure<GraphDrawData>>();
    }

    [Fact]
    public void ToByteArray_and_FromByteArray_round_trips_the_graph()
    {
        var original = CreateValid().Should().BeOfType<Success<GraphDrawData>>().Subject.Value;

        var roundTripped = GraphDrawData.FromByteArray(original.ToByteArray())
            .Should().BeOfType<Success<GraphDrawData>>().Subject.Value;

        roundTripped.ChannelAName.Should().Be(original.ChannelAName);
        roundTripped.ChannelBName.Should().Be(original.ChannelBName);
        roundTripped.ChannelABinEdges.Should().Equal(original.ChannelABinEdges);
        roundTripped.ChannelBBinEdges.Should().Equal(original.ChannelBBinEdges);
        roundTripped.CellColorsRowMajor.Should().Equal(original.CellColorsRowMajor);
        roundTripped.DrawAxisTickLabels.Should().Be(original.DrawAxisTickLabels);
        roundTripped.DrawAxisTitles.Should().Be(original.DrawAxisTitles);
    }
}
