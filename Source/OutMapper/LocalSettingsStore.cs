using Windows.Storage;

namespace OutMapper;

/// <summary>
/// Real <see cref="ISettingsStore"/> implementation backed by <see cref="ApplicationData.Current"/>.
/// </summary>
internal sealed class LocalSettingsStore : ISettingsStore
{
    public static readonly LocalSettingsStore Instance = new();

    private LocalSettingsStore()
    {
    }

    public string? GetString(string key)
    {
        return ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out var value)
            ? value as string
            : null;
    }

    public void SetString(string key, string value)
    {
        ApplicationData.Current.LocalSettings.Values[key] = value;
    }

    public void Remove(string key)
    {
        ApplicationData.Current.LocalSettings.Values.Remove(key);
    }
}
