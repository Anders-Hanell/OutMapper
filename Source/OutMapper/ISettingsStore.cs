namespace OutMapper;

/// <summary>
/// Seam over persisted app settings (backed by <c>ApplicationData.Current.LocalSettings</c> in production),
/// so tests can substitute an in-memory implementation.
/// </summary>
internal interface ISettingsStore
{
    string? GetString(string key);

    void SetString(string key, string value);

    void Remove(string key);
}
