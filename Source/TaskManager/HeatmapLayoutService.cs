namespace TaskManager;

using Algorithms;
using DataStructures;

/// <summary>
/// Forwards to TaskManager's Algorithms reference, which owns the actual, pure, Skia-free heatmap
/// geometry computation. Called only from <see cref="TaskManagerService.HandleComputeHeatmapLayoutRequestAsync"/>
/// — OutMapper reaches this via a <see cref="Messages.ComputeHeatmapLayoutRequest"/>/<see cref="Messages.ComputeHeatmapLayoutResponse"/>
/// round trip through the gateway, correlated by request id, rather than calling in directly.
/// </summary>
internal static class HeatmapLayoutService
{
    internal static Result<HeatmapLayoutData> ComputeLayout(
        GraphDrawData data, double areaLeft, double areaTop, double areaWidth, double areaHeight) =>
        HeatmapLayout.Compute(data, areaLeft, areaTop, areaWidth, areaHeight);

    internal static (double ReservedLeft, double ReservedBottom) ComputeReservedMargins(GraphDrawData data) =>
        HeatmapLayout.ComputeReservedMargins(data);
}
