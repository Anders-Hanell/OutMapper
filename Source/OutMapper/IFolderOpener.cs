namespace OutMapper;

/// <summary>
/// Seam over "open a folder in the operating system's file explorer" (backed by
/// <c>Windows.System.Launcher</c> in production), so tests can substitute a fake instead of
/// launching a real OS window.
/// </summary>
internal interface IFolderOpener
{
    Task<bool> OpenFolderAsync(string folderPath);
}
