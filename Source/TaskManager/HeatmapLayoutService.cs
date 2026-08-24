namespace TaskManager;

using Algorithms;
using DataStructures;

/// <summary>
/// Forwards to TaskManager's Algorithms reference, which owns the actual, pure, Skia-free heatmap
/// geometry computation. Called by <see cref="HeatmapDrawing"/>, which paints from the geometry this
/// returns — both live in TaskManager, so this is a plain in-process call, not a message.
/// </summary>
internal static class HeatmapLayoutService
{
    internal static Result<HeatmapLayoutData> ComputeLayout(
        GraphDrawData data, double areaLeft, double areaTop, double areaWidth, double areaHeight) =>
        HeatmapLayout.Compute(data, areaLeft, areaTop, areaWidth, areaHeight);

    internal static (double ReservedLeft, double ReservedBottom) ComputeReservedMargins(GraphDrawData data) =>
        HeatmapLayout.ComputeReservedMargins(data);
}
