using OutMapper;

namespace TestSupport;

/// <summary>
/// In-memory <see cref="ISettingsStore"/> fake for tests. Each instance is independent.
/// </summary>
public sealed class InMemorySettingsStore : ISettingsStore
{
    private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);

    public string? GetString(string key) => _values.TryGetValue(key, out var value) ? value : null;

    public void SetString(string key, string value) => _values[key] = value;

    public void Remove(string key) => _values.Remove(key);
}
