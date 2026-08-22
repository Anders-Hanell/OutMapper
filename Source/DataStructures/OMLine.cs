namespace DataStructures;

/// <summary>
/// A straight line segment between two points, drawn in its own color and width. Prefixed OM
/// (OutMapper) to avoid clashing with the same-named types Uno and SkiaSharp bring into scope in
/// OutMapper.
/// </summary>
public sealed record OMLine(OMPoint Start, OMPoint End, OMColor LineColor, double LineWidth);
