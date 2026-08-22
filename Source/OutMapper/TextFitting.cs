using DataStructures;
using SkiaSharp;

namespace OutMapper;

/// <summary>
/// Computes the largest <see cref="SKFont.Size"/> that fits given text inside a max-width/max-height
/// budget, using a single <see cref="SKFont.MeasureText(string, out SKRect, SKPaint?)"/> probe and
/// linear scaling — SkiaSharp glyph metrics scale exactly linearly with <see cref="SKFont.Size"/> for a
/// fixed typeface, so no iteration/binary search is needed. Used by <see cref="HeatmapDrawing"/> to size
/// tick labels and axis titles to their region instead of a hardcoded <see cref="SKFont.Size"/>.
/// </summary>
internal static class TextFitting
{
    private const float ProbeFontSize = 100f;
    private const float DefaultSafetyFactor = 0.9f;

    internal static float FitSize(string? text, float maxWidth, float maxHeight, SKTypeface? typeface = null)
    {
        if (string.IsNullOrWhiteSpace(text) || maxWidth <= 0f || maxHeight <= 0f)
        {
            return 0f;
        }

        using var probeFont = new SKFont(typeface, ProbeFontSize);
        probeFont.MeasureText(text, out var bounds);

        if (bounds.Width <= 0f || bounds.Height <= 0f)
        {
            return 0f;
        }

        return ProbeFontSize * Math.Min(maxWidth / bounds.Width, maxHeight / bounds.Height);
    }

    /// <summary>
    /// The one size that makes every item fit its own budget — the min of <see cref="FitSize"/> across
    /// the group — so a set of labels (e.g. every tick across both heatmap axes, or both axis titles)
    /// renders uniformly instead of each shrinking independently. 0 for an empty collection.
    /// </summary>
    internal static float FitUniformSize(
        IReadOnlyCollection<(string Text, float MaxWidth, float MaxHeight)> items, SKTypeface? typeface = null)
    {
        if (items.Count == 0)
        {
            return 0f;
        }

        var size = float.PositiveInfinity;
        foreach (var (text, maxWidth, maxHeight) in items)
        {
            size = Math.Min(size, FitSize(text, maxWidth, maxHeight, typeface));
        }

        return size;
    }

    /// <summary>
    /// The one size that makes every box in the group fit its own on-page footprint (<see cref="OMRect"/>),
    /// honoring each box's <see cref="OMTextRotation"/> — a <see cref="OMTextRotation.CounterClockwise90"/>
    /// box's Rect.Height (its on-page long dimension) becomes the text-flow-direction budget, and its
    /// Rect.Width becomes the thickness budget, since the rect describes the rotated footprint rather
    /// than the pre-rotation reading-direction box. Each raw budget is shrunk by safetyFactor first so
    /// fitted text doesn't touch the edges of its box.
    /// </summary>
    internal static float FitUniformSize(
        IReadOnlyCollection<OMTextBox> boxes, float safetyFactor = DefaultSafetyFactor, SKTypeface? typeface = null)
    {
        var items = new (string Text, float MaxWidth, float MaxHeight)[boxes.Count];
        var i = 0;
        foreach (var box in boxes)
        {
            var (rawWidth, rawHeight) = box.Rotation == OMTextRotation.CounterClockwise90
                ? (box.Rect.Height, box.Rect.Width)
                : (box.Rect.Width, box.Rect.Height);

            items[i++] = (box.Text, (float)(rawWidth * safetyFactor), (float)(rawHeight * safetyFactor));
        }

        return FitUniformSize(items, typeface);
    }
}
