namespace DataStructures;

/// <summary>How an <see cref="OMTextBox"/>'s text is oriented within its <see cref="OMRect"/>.</summary>
public enum OMTextRotation
{
    Horizontal,
    CounterClockwise90
}

/// <summary>
/// A piece of text, centered within Rect. Rect describes the text's actual on-page footprint — for a
/// <see cref="OMTextRotation.CounterClockwise90"/> box this is already the rotated (tall-and-thin)
/// footprint, not the pre-rotation reading-direction box, so Rect always bounds what's visually drawn.
/// Text color travels on Rect's own FillColor rather than a separate field. Prefixed OM (OutMapper) to
/// avoid clashing with the same-named types Uno and SkiaSharp bring into scope in OutMapper.
/// </summary>
public sealed record OMTextBox(string Text, OMRect Rect, OMTextRotation Rotation);
