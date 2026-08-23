using System.Collections.Immutable;
using DataStructures;
using Messages;
using TestSupport;

namespace OutMapper.Tests;

public class GatewayToTaskManagerTests
{
    [Fact]
    public async Task SendMessage_round_trips_a_correlated_request_through_TaskManager_and_back_through_the_gateway()
    {
        GatewayTestHarness.EnsureInitialized();

        var data = GraphDrawData.Create(
            channelAName: "A",
            channelBName: "B",
            channelABinEdges: ImmutableArray.Create(0.0, 1.0),
            channelBBinEdges: ImmutableArray.Create(0.0, 1.0),
            cellColorsRowMajor: ImmutableArray.Create("#FF0000"),
            drawAxisTickLabels: false,
            drawAxisTitles: false).Should().BeOfType<Success<GraphDrawData>>().Subject.Value;

        var request = new ComputeHeatmapLayoutRequest(Guid.NewGuid(), data, 0, 0, 100, 100);

        var response = await GatewayRequestCorrelator
            .SendAsync<ComputeHeatmapLayoutRequest, ComputeHeatmapLayoutResponse>(request);

        response.RequestId.Should().Be(request.RequestId);
        response.Result.Should().BeOfType<Success<HeatmapLayoutData>>();
    }
}
