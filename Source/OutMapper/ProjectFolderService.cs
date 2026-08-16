using System.IO;
using TaskManager;

namespace OutMapper;

internal static class ProjectFolderService
{
    private const string SelectedProjectNameKey = "SelectedProjectName";
    private const string SelectedProjectWorkspacePathKey = "SelectedProjectWorkspacePath";
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

    public static string[] GetProjectNames(out string? error) =>
        GetProjectNames(LocalFileSystem.Instance, SettingsWorkspaceContent.LoadWorkspaceFolderPath(), out error);

    internal static string[] GetProjectNames(IFileSystem fileSystem, string? workspaceFolder, out string? error)
    {
        if (string.IsNullOrWhiteSpace(workspaceFolder) || !fileSystem.DirectoryExists(workspaceFolder))
        {
            error = "Select a valid workspace before viewing projects.";
            return [];
        }

        var projectsFolder = System.IO.Path.Combine(workspaceFolder, "Projects");
        if (!fileSystem.DirectoryExists(projectsFolder))
        {
            error = null;
            return [];
        }

        try
        {
            error = null;
            return fileSystem.GetDirectories(projectsFolder)
                .Select(System.IO.Path.GetFileName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Cast<string>()
                .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            error = $"Unable to read projects: {exception.Message}";
            return [];
        }
    }

    public static bool TryCreateProject(string? proposedName, out string message) =>
        TryCreateProject(LocalFileSystem.Instance, SettingsWorkspaceContent.LoadWorkspaceFolderPath(), proposedName, out message);

    internal static bool TryCreateProject(IFileSystem fileSystem, string? workspaceFolder, string? proposedName, out string message)
    {
        if (string.IsNullOrWhiteSpace(workspaceFolder) || !fileSystem.DirectoryExists(workspaceFolder))
        {
            message = "Select a valid workspace before creating a project.";
            return false;
        }

        var projectName = proposedName?.Trim();
        if (!IsValidFolderName(projectName, out message))
        {
            return false;
        }

        var projectsFolder = System.IO.Path.Combine(workspaceFolder, "Projects");
        var projectFolder = System.IO.Path.Combine(projectsFolder, projectName!);

        try
        {
            if (fileSystem.DirectoryExists(projectFolder))
            {
                message = $"A project named '{projectName}' already exists.";
                return false;
            }

            fileSystem.CreateDirectory(projectFolder);
            fileSystem.CreateDirectory(System.IO.Path.Combine(projectFolder, InternalFilesFolderName));
            fileSystem.CreateDirectory(System.IO.Path.Combine(projectFolder, ProjectOutputFolderName));
            message = $"Project '{projectName}' was created successfully.";
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            message = $"Unable to create project: {exception.Message}";
            return false;
        }
    }

    public static string? GetSelectedProjectName(out string? error) =>
        GetSelectedProjectName(
            LocalFileSystem.Instance, LocalSettingsStore.Instance, SettingsWorkspaceContent.LoadWorkspaceFolderPath(), out error);

    internal static string? GetSelectedProjectName(
        IFileSystem fileSystem, ISettingsStore settingsStore, string? workspaceFolder, out string? error)
    {
        if (string.IsNullOrWhiteSpace(workspaceFolder) || !fileSystem.DirectoryExists(workspaceFolder))
        {
            error = "Select a valid workspace before selecting a project.";
            return null;
        }

        var selectedWorkspace = settingsStore.GetString(SelectedProjectWorkspacePathKey);
        var selectedProject = settingsStore.GetString(SelectedProjectNameKey);

        if (!string.Equals(selectedWorkspace, workspaceFolder, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(selectedProject))
        {
            error = null;
            return null;
        }

        var projectFolder = System.IO.Path.Combine(workspaceFolder, "Projects", selectedProject);
        if (!fileSystem.DirectoryExists(projectFolder))
        {
            ClearSelectedProject(settingsStore);
            error = $"The selected project '{selectedProject}' no longer exists.";
            return null;
        }

        error = null;
        return selectedProject;
    }

    public static bool TrySelectProject(string? projectName, out string message) =>
        TrySelectProject(
            LocalFileSystem.Instance, LocalSettingsStore.Instance, SettingsWorkspaceContent.LoadWorkspaceFolderPath(),
            projectName, out message);

    internal static bool TrySelectProject(
        IFileSystem fileSystem, ISettingsStore settingsStore, string? workspaceFolder, string? projectName, out string message)
    {
        if (string.IsNullOrWhiteSpace(workspaceFolder) || !fileSystem.DirectoryExists(workspaceFolder))
        {
            message = "Select a valid workspace before selecting a project.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(projectName))
        {
            message = "Choose a project.";
            return false;
        }

        var availableProjects = GetProjectNames(fileSystem, workspaceFolder, out var error);
        if (error is not null)
        {
            message = error;
            return false;
        }

        var selectedProject = availableProjects.FirstOrDefault(
            name => string.Equals(name, projectName, StringComparison.Ordinal));
        if (selectedProject is null)
        {
            message = $"The project '{projectName}' does not exist in the current workspace.";
            return false;
        }

        settingsStore.SetString(SelectedProjectNameKey, selectedProject);
        settingsStore.SetString(SelectedProjectWorkspacePathKey, workspaceFolder);
        message = $"Project '{selectedProject}' is now selected.";
        return true;
    }

    public static void ClearSelectedProject() => ClearSelectedProject(LocalSettingsStore.Instance);

    internal static void ClearSelectedProject(ISettingsStore settingsStore)
    {
        settingsStore.Remove(SelectedProjectNameKey);
        settingsStore.Remove(SelectedProjectWorkspacePathKey);
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
