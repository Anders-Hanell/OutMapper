namespace TaskManager;

using Algorithms;
using DataStructures;

/// <summary>
/// A plain synchronous forwarding call from OutMapper into TaskManager's Algorithms reference, so
/// OutMapper never needs a project reference to Algorithms itself (which owns the actual, pure,
/// Skia-free heatmap geometry computation). Internal like every other class here — reachable from
/// OutMapper only via the <c>InternalsVisibleTo</c> grant in TaskManager.csproj, not by being public.
/// Deliberately synchronous rather than going through the async Message/GatewayToOutMapper pipeline:
/// that pipeline has no built-in request/response correlation (every existing flow is routed by
/// message type to one singleton UI screen) and depends on a live UI DispatcherQueue having been wired
/// up via GatewayToTaskManager.Initialize(), which isn't available in a plain test host — round-tripping
/// this trivial, pure computation through it would make it untestable without new test-only plumbing.
/// </summary>
internal static class HeatmapLayoutService
{
    internal static Result<HeatmapLayoutData> ComputeLayout(
        GraphDrawData data, double areaLeft, double areaTop, double areaWidth, double areaHeight) =>
        HeatmapLayout.Compute(data, areaLeft, areaTop, areaWidth, areaHeight);

    internal static (double ReservedLeft, double ReservedBottom) ComputeReservedMargins(GraphDrawData data) =>
        HeatmapLayout.ComputeReservedMargins(data);
}
