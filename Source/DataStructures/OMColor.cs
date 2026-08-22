namespace DataStructures;

/// <summary>
/// An RGB color. Prefixed OM (OutMapper) to avoid clashing with the same-named types Uno and
/// SkiaSharp bring into scope in OutMapper.
/// </summary>
public sealed record OMColor(byte R, byte G, byte B);
