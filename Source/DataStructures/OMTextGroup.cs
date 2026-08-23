using System.Collections.Immutable;

namespace DataStructures;

/// <summary>
/// A set of <see cref="OMTextBox"/>es meant to be rendered with one shared font size — e.g. every axis
/// tick label, or both axis titles — so a drawing engine can size the group as a whole (the smallest fit
/// across every box's own footprint) without knowing what the text represents. Prefixed OM (OutMapper)
/// to avoid clashing with the same-named types Uno and SkiaSharp bring into scope in OutMapper.
/// </summary>
public sealed record OMTextGroup(ImmutableArray<OMTextBox> Boxes);
