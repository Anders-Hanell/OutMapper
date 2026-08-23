namespace TaskManager;

using Algorithms;
using DataStructures;

/// <summary>
/// Forwards to TaskManager's Algorithms reference, which owns the actual, pure, Skia-free figure
/// geometry computation. Called only from <see cref="TaskManagerService.HandleComputeFigureLayoutRequestAsync"/>
/// — OutMapper reaches this via a <see cref="Messages.ComputeFigureLayoutRequest"/>/<see cref="Messages.ComputeFigureLayoutResponse"/>
/// round trip through the gateway, correlated by request id, rather than calling in directly.
/// </summary>
internal static class FigureLayoutService
{
    internal static Result<FigureLayoutData> ComputeLayout(FigureDrawData figure) =>
        FigureLayout.Compute(figure);
}
