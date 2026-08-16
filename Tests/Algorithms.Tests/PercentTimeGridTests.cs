namespace Algorithms.Tests;

public class PercentTimeGridTests
{
    private const float MissingValue = -9999f;

    [Fact]
    public void ComputePercentTimeMatrix_excludes_values_outside_the_bin_edges_from_the_percentage()
    {
        var channelABinEdges = GridBinning.ComputeBinEdges(min: 0, max: 10, binSize: 5);
        var channelBBinEdges = GridBinning.ComputeBinEdges(min: 0, max: 10, binSize: 5);

        // Second observation's channel A value (20) falls outside the configured range and should
        // be treated as missing, not counted in the percentage denominator.
        var channelAValues = new List<float> { 2f, 20f };
        var channelBValues = new List<float> { 2f, 2f };

        var matrix = PercentTimeGrid.ComputePercentTimeMatrix(
            channelAValues, channelBValues, channelABinEdges, channelBBinEdges, MissingValue);

        matrix.Should().NotBeNull();
        matrix![0, 0].Should().Be(100.0);
    }

    [Fact]
    public void ComputePercentTimeMatrix_returns_null_when_all_observations_fall_outside_the_range()
    {
        var channelABinEdges = GridBinning.ComputeBinEdges(min: 0, max: 10, binSize: 5);
        var channelBBinEdges = GridBinning.ComputeBinEdges(min: 0, max: 10, binSize: 5);

        var channelAValues = new List<float> { 20f };
        var channelBValues = new List<float> { 20f };

        var matrix = PercentTimeGrid.ComputePercentTimeMatrix(
            channelAValues, channelBValues, channelABinEdges, channelBBinEdges, MissingValue);

        matrix.Should().BeNull();
    }
}
