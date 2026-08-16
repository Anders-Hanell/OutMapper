namespace OutMapper;

/// <summary>
/// Real <see cref="IContentHost"/> implementation forwarding to a live <see cref="ContentControl"/>.
/// </summary>
internal sealed class ContentControlHost : IContentHost
{
    private readonly ContentControl _contentControl;

    public ContentControlHost(ContentControl contentControl)
    {
        _contentControl = contentControl;
    }

    public object? Content
    {
        get => _contentControl.Content;
        set => _contentControl.Content = value;
    }
}
