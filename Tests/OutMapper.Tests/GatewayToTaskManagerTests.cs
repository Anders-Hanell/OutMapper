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

        var request = new RenderFigurePreviewRequest(
            Guid.NewGuid(), ProjectFolder: "/does/not/exist", RowCount: 1, ColCount: 1,
            CellAnalysisNames: ImmutableArray.Create((string?)null), FigureLabelStyle.None, MaxDimensionPx: 100);

        var response = await GatewayRequestCorrelator
            .SendAsync<RenderFigurePreviewRequest, RenderFigurePreviewResponse>(request);

        response.RequestId.Should().Be(request.RequestId);
        response.Result.Should().BeOfType<Success<byte[]>>();
    }
}
