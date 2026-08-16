namespace Algorithms.Tests;

public class GridBinningTests
{
    [Fact]
    public void ComputeBinEdges_returns_edges_spanning_min_to_max()
    {
        var edges = GridBinning.ComputeBinEdges(min: 0, max: 10, binSize: 5);

        edges.Should().Equal(0, 5, 10);
    }
}
