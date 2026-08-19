using OutMapper;

namespace TestSupport;

/// <summary>
/// <see cref="IFolderOpener"/> fake that records the requested path instead of launching a real OS window.
/// </summary>
public sealed class FakeFolderOpener : IFolderOpener
{
    public List<string> OpenedFolderPaths { get; } = new();
    public bool ResultToReturn { get; set; } = true;

    public Task<bool> OpenFolderAsync(string folderPath)
    {
        OpenedFolderPaths.Add(folderPath);
        return Task.FromResult(ResultToReturn);
    }
}
