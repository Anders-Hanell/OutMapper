namespace DataStructures;

/// <summary>
/// An axis-aligned rectangle: all four corners plus width and height (redundant with the corners,
/// but keeps drawing code from having to recompute them), filled with a single color. Prefixed OM
/// (OutMapper) to avoid clashing with the same-named types Uno and SkiaSharp bring into scope in
/// OutMapper.
/// </summary>
public sealed record OMRect(
    OMPoint TopLeft, OMPoint TopRight, OMPoint BottomLeft, OMPoint BottomRight,
    double Width, double Height, OMColor FillColor);
