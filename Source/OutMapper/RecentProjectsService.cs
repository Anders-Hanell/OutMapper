using System.Collections.Immutable;
using System.Text.Json;
using TaskManager;
using Path = System.IO.Path;

namespace OutMapper;

internal static class RecentProjectsService
{
    private const string RecentProjectFoldersKey = "RecentProjectFolders";
    private const int MaxEntries = 10;

    internal readonly record struct Entry(string Folder, string Name, bool IsMissing);

    public static ImmutableArray<Entry> GetRecentProjects() =>
        GetRecentProjects(LocalFileSystem.Instance, LocalSettingsStore.Instance);

    internal static ImmutableArray<Entry> GetRecentProjects(IFileSystem fileSystem, ISettingsStore settingsStore) =>
        ReadFolders(settingsStore)
            .Select(folder => new Entry(folder, Path.GetFileName(folder), !fileSystem.DirectoryExists(folder)))
            .ToImmutableArray();

    public static void AddOrPromote(string folder) => AddOrPromote(LocalSettingsStore.Instance, folder);

    internal static void AddOrPromote(ISettingsStore settingsStore, string folder)
    {
        var folders = ReadFolders(settingsStore)
            .Where(existing => !string.Equals(existing, folder, StringComparison.OrdinalIgnoreCase))
            .ToList();

        folders.Insert(0, folder);
        WriteFolders(settingsStore, folders.Take(MaxEntries));
    }

    public static void Remove(string folder) => Remove(LocalSettingsStore.Instance, folder);

    internal static void Remove(ISettingsStore settingsStore, string folder)
    {
        var folders = ReadFolders(settingsStore)
            .Where(existing => !string.Equals(existing, folder, StringComparison.OrdinalIgnoreCase));

        WriteFolders(settingsStore, folders);
    }

    private static List<string> ReadFolders(ISettingsStore settingsStore)
    {
        var json = settingsStore.GetString(RecentProjectFoldersKey);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static void WriteFolders(ISettingsStore settingsStore, IEnumerable<string> folders)
    {
        settingsStore.SetString(RecentProjectFoldersKey, JsonSerializer.Serialize(folders.ToList()));
    }
}
