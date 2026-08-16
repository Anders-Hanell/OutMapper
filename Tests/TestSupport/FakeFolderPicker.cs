using OutMapper;

namespace TestSupport;

/// <summary>
/// <see cref="IFolderPicker"/> fake that returns a preconfigured path instead of showing a live dialog.
/// </summary>
public sealed class FakeFolderPicker : IFolderPicker
{
    public string? PathToReturn { get; set; }

    public Task<string?> PickFolderAsync() => Task.FromResult(PathToReturn);
}
