namespace DataStructures;

/// <summary>
/// A 2D coordinate. Prefixed OM (OutMapper) to avoid clashing with the same-named types Uno and
/// SkiaSharp bring into scope in OutMapper.
/// </summary>
public sealed record OMPoint(double X, double Y);
