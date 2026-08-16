using System.IO;
using TaskManager;

namespace OutMapper;

internal static class ProjectFolderService
{
    private const string CurrentProjectFolderKey = "CurrentProjectFolder";
    internal const string InternalFilesFolderName = "OutMapper_InternalFiles";
    internal const string ProjectOutputFolderName = "OutMapper_ProjectOutput";

    private static readonly char[] InvalidFolderNameCharacters =
        System.IO.Path.GetInvalidFileNameChars()
            .Concat(['<', '>', ':', '"', '/', '\\', '|', '?', '*'])
            .Distinct()
            .ToArray();

    private static readonly HashSet<string> ReservedWindowsNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    public static string? GetCurrentProjectFolder() =>
        GetCurrentProjectFolder(LocalFileSystem.Instance, LocalSettingsStore.Instance);

    internal static string? GetCurrentProjectFolder(IFileSystem fileSystem, ISettingsStore settingsStore)
    {
        var projectFolder = settingsStore.GetString(CurrentProjectFolderKey);
        if (string.IsNullOrWhiteSpace(projectFolder))
        {
            return null;
        }

        if (!fileSystem.DirectoryExists(projectFolder))
        {
            settingsStore.Remove(CurrentProjectFolderKey);
            return null;
        }

        return projectFolder;
    }

    public static string? GetCurrentProjectName() =>
        GetCurrentProjectName(LocalFileSystem.Instance, LocalSettingsStore.Instance);

    internal static string? GetCurrentProjectName(IFileSystem fileSystem, ISettingsStore settingsStore)
    {
        var projectFolder = GetCurrentProjectFolder(fileSystem, settingsStore);
        return projectFolder is null ? null : System.IO.Path.GetFileName(projectFolder);
    }

    public static bool TryCreateProject(string? parentFolder, string? proposedName, out string message) =>
        TryCreateProject(LocalFileSystem.Instance, LocalSettingsStore.Instance, parentFolder, proposedName, out message);

    internal static bool TryCreateProject(
        IFileSystem fileSystem, ISettingsStore settingsStore, string? parentFolder, string? proposedName, out string message)
    {
        if (string.IsNullOrWhiteSpace(parentFolder) || !fileSystem.DirectoryExists(parentFolder))
        {
            message = "Select a valid location before creating a project.";
            return false;
        }

        var projectName = proposedName?.Trim();
        if (!IsValidFolderName(projectName, out message))
        {
            return false;
        }

        var projectFolder = System.IO.Path.Combine(parentFolder, projectName!);

        try
        {
            if (fileSystem.DirectoryExists(projectFolder))
            {
                message = $"A project named '{projectName}' already exists at that location.";
                return false;
            }

            fileSystem.CreateDirectory(projectFolder);
            fileSystem.CreateDirectory(System.IO.Path.Combine(projectFolder, InternalFilesFolderName));
            fileSystem.CreateDirectory(System.IO.Path.Combine(projectFolder, ProjectOutputFolderName));

            SetCurrentProject(settingsStore, projectFolder);

            message = $"Project '{projectName}' was created successfully.";
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            message = $"Unable to create project: {exception.Message}";
            return false;
        }
    }

    public static bool TryOpenProject(string? folder, out string message) =>
        TryOpenProject(LocalFileSystem.Instance, LocalSettingsStore.Instance, folder, out message);

    internal static bool TryOpenProject(IFileSystem fileSystem, ISettingsStore settingsStore, string? folder, out string message)
    {
        if (string.IsNullOrWhiteSpace(folder) || !fileSystem.DirectoryExists(folder))
        {
            message = "Select a valid project folder.";
            return false;
        }

        if (!fileSystem.DirectoryExists(System.IO.Path.Combine(folder, InternalFilesFolderName)))
        {
            message = "That folder doesn't look like an OutMapper project.";
            return false;
        }

        SetCurrentProject(settingsStore, folder);

        message = $"Project '{System.IO.Path.GetFileName(folder)}' is now open.";
        return true;
    }

    public static void ClearCurrentProject() => ClearCurrentProject(LocalSettingsStore.Instance);

    internal static void ClearCurrentProject(ISettingsStore settingsStore)
    {
        settingsStore.Remove(CurrentProjectFolderKey);
    }

    private static void SetCurrentProject(ISettingsStore settingsStore, string projectFolder)
    {
        settingsStore.SetString(CurrentProjectFolderKey, projectFolder);
        RecentProjectsService.AddOrPromote(settingsStore, projectFolder);
    }

    private static bool IsValidFolderName(string? name, out string message)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            message = "Enter a project name.";
            return false;
        }

        if (name is "." or ".." || name.IndexOfAny(InvalidFolderNameCharacters) >= 0 ||
            name.Any(char.IsControl) || name.EndsWith(' ') || name.EndsWith('.'))
        {
            message = "Enter a valid folder name without reserved characters or a trailing space or period.";
            return false;
        }

        var nameWithoutExtension = name.Split('.')[0];
        if (ReservedWindowsNames.Contains(nameWithoutExtension))
        {
            message = "That project name is reserved by the operating system.";
            return false;
        }

        message = string.Empty;
        return true;
    }
}
