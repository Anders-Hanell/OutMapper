namespace OutMapper;

/// <summary>
/// Seam over "let the user choose a folder" (backed by <c>Windows.Storage.Pickers.FolderPicker</c> in
/// production), so tests can substitute a preconfigured path instead of a live file dialog.
/// </summary>
internal interface IFolderPicker
{
    Task<string?> PickFolderAsync();
}
