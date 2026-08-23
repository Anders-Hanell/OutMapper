namespace TaskManager;

using Algorithms;
using DataStructures;

/// <summary>
/// A plain synchronous forwarding call from OutMapper into TaskManager's Algorithms reference, so
/// OutMapper never needs a project reference to Algorithms itself (which owns the actual, pure,
/// Skia-free figure geometry computation). Internal like every other class here — reachable from
/// OutMapper only via the <c>InternalsVisibleTo</c> grant in TaskManager.csproj, not by being public.
/// See <c>HeatmapLayoutService</c> for the full rationale (deliberately not routed through the async
/// Message/GatewayToOutMapper pipeline).
/// </summary>
internal static class FigureLayoutService
{
    internal static Result<FigureLayoutData> ComputeLayout(FigureDrawData figure) =>
        FigureLayout.Compute(figure);
}
