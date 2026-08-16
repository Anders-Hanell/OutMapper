using Windows.Storage.Pickers;

namespace OutMapper;

/// <summary>
/// Real <see cref="IFolderPicker"/> implementation wrapping <see cref="FolderPicker"/>.
/// </summary>
internal sealed class WindowsFolderPicker : IFolderPicker
{
    public async Task<string?> PickFolderAsync()
    {
        var picker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.Desktop
        };
        picker.FileTypeFilter.Add("*");
        var folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }
}
