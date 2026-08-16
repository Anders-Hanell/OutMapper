namespace OutMapper;

/// <summary>
/// Seam over "let the user choose a file" (backed by <c>Windows.Storage.Pickers.FileOpenPicker</c> in
/// production), so tests can substitute a preconfigured path instead of a live file dialog.
/// </summary>
internal interface IFilePicker
{
    Task<string?> PickFileAsync();
}
