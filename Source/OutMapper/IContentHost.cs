namespace OutMapper;

/// <summary>
/// The minimal "what's currently on screen" seam <see cref="NavigationManager"/> depends on, so it can be
/// unit tested without a live Uno control tree.
/// </summary>
internal interface IContentHost
{
    object? Content { get; set; }
}
