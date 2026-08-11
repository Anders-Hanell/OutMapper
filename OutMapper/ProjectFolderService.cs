using System.IO;
using Windows.Storage;

namespace OutMapper;

internal static class ProjectFolderService
{
    private const string SelectedProjectNameKey = "SelectedProjectName";
    private const string SelectedProjectWorkspacePathKey = "SelectedProjectWorkspacePath";

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

    public static string[] GetProjectNames(out string? error)
    {
        var workspaceFolder = SettingsWorkspaceContent.LoadWorkspaceFolderPath();
        if (string.IsNullOrWhiteSpace(workspaceFolder) || !Directory.Exists(workspaceFolder))
        {
            error = "Select a valid workspace before viewing projects.";
            return [];
        }

        var projectsFolder = System.IO.Path.Combine(workspaceFolder, "Projects");
        if (!Directory.Exists(projectsFolder))
        {
            error = null;
            return [];
        }

        try
        {
            error = null;
            return Directory.GetDirectories(projectsFolder, "*", SearchOption.TopDirectoryOnly)
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

    public static bool TryCreateProject(string? proposedName, out string message)
    {
        var workspaceFolder = SettingsWorkspaceContent.LoadWorkspaceFolderPath();
        if (string.IsNullOrWhiteSpace(workspaceFolder) || !Directory.Exists(workspaceFolder))
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
            if (Directory.Exists(projectFolder))
            {
                message = $"A project named '{projectName}' already exists.";
                return false;
            }

            Directory.CreateDirectory(projectFolder);
            message = $"Project '{projectName}' was created successfully.";
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            message = $"Unable to create project: {exception.Message}";
            return false;
        }
    }

    public static string? GetSelectedProjectName(out string? error)
    {
        var workspaceFolder = SettingsWorkspaceContent.LoadWorkspaceFolderPath();
        if (string.IsNullOrWhiteSpace(workspaceFolder) || !Directory.Exists(workspaceFolder))
        {
            error = "Select a valid workspace before selecting a project.";
            return null;
        }

        var settings = ApplicationData.Current.LocalSettings.Values;
        var selectedWorkspace = settings.TryGetValue(SelectedProjectWorkspacePathKey, out var workspaceValue)
            ? workspaceValue as string
            : null;
        var selectedProject = settings.TryGetValue(SelectedProjectNameKey, out var projectValue)
            ? projectValue as string
            : null;

        if (!string.Equals(selectedWorkspace, workspaceFolder, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(selectedProject))
        {
            error = null;
            return null;
        }

        var projectFolder = System.IO.Path.Combine(workspaceFolder, "Projects", selectedProject);
        if (!Directory.Exists(projectFolder))
        {
            ClearSelectedProject();
            error = $"The selected project '{selectedProject}' no longer exists.";
            return null;
        }

        error = null;
        return selectedProject;
    }

    public static bool TrySelectProject(string? projectName, out string message)
    {
        var workspaceFolder = SettingsWorkspaceContent.LoadWorkspaceFolderPath();
        if (string.IsNullOrWhiteSpace(workspaceFolder) || !Directory.Exists(workspaceFolder))
        {
            message = "Select a valid workspace before selecting a project.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(projectName))
        {
            message = "Choose a project.";
            return false;
        }

        var availableProjects = GetProjectNames(out var error);
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

        var settings = ApplicationData.Current.LocalSettings.Values;
        settings[SelectedProjectNameKey] = selectedProject;
        settings[SelectedProjectWorkspacePathKey] = workspaceFolder;
        message = $"Project '{selectedProject}' is now selected.";
        return true;
    }

    public static void ClearSelectedProject()
    {
        var settings = ApplicationData.Current.LocalSettings.Values;
        settings.Remove(SelectedProjectNameKey);
        settings.Remove(SelectedProjectWorkspacePathKey);
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
