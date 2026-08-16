using Windows.Storage.Pickers;

namespace OutMapper;

/// <summary>
/// Real <see cref="IFilePicker"/> implementation wrapping <see cref="FileOpenPicker"/>, filtered to CSV files.
/// </summary>
internal sealed class WindowsCsvFilePicker : IFilePicker
{
    public async Task<string?> PickFileAsync()
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.Desktop
        };
        picker.FileTypeFilter.Add(".csv");
        var file = await picker.PickSingleFileAsync();
        return file?.Path;
    }
}
