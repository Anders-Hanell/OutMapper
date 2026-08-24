namespace TaskManager;

using Algorithms;
using DataStructures;

/// <summary>
/// Forwards to TaskManager's Algorithms reference, which owns the actual, pure, Skia-free figure
/// geometry computation. Called by <see cref="FigurePdfGenerator"/> and <see cref="FigurePreviewRenderer"/>,
/// which paint from the geometry this returns — both live in TaskManager, so this is a plain in-process
/// call, not a message.
/// </summary>
internal static class FigureLayoutService
{
    internal static Result<FigureLayoutData> ComputeLayout(FigureDrawData figure) =>
        FigureLayout.Compute(figure);
}
