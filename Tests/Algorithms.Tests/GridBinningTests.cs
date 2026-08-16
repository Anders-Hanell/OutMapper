namespace Algorithms.Tests;

public class GridBinningTests
{
    [Fact]
    public void ComputeBinEdges_returns_edges_spanning_min_to_max()
    {
        var edges = GridBinning.ComputeBinEdges(min: 0, max: 10, binSize: 5);

        edges.Should().Equal(0, 5, 10);
    }

    [Fact]
    public void ComputeBinEdges_narrows_the_last_bin_when_the_span_is_not_a_whole_number_of_bins()
    {
        var edges = GridBinning.ComputeBinEdges(min: 0, max: 12, binSize: 5);

        edges.Should().Equal(0, 5, 10, 12);
    }

    [Fact]
    public void FindBinIndex_returns_null_for_values_outside_the_edges()
    {
        var edges = GridBinning.ComputeBinEdges(min: 0, max: 10, binSize: 5);

        GridBinning.FindBinIndex(edges, value: -1).Should().BeNull();
        GridBinning.FindBinIndex(edges, value: 11).Should().BeNull();
    }
}
