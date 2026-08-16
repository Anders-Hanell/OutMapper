using Microsoft.UI.Text;
using TaskManager;

namespace OutMapper;

public sealed class SettingsRecentProjectsContent : Border
{
    private readonly IFileSystem _fileSystem;
    private readonly ISettingsStore _settingsStore;
    private readonly IFolderPicker _folderPicker;
    private readonly TextBlock _currentProjectLabel;
    private readonly StackPanel _recentProjectsList;
    private readonly TextBlock _resultLabel;

    public SettingsRecentProjectsContent()
        : this(LocalFileSystem.Instance, LocalSettingsStore.Instance, new WindowsFolderPicker())
    {
    }

    internal SettingsRecentProjectsContent(IFileSystem fileSystem, ISettingsStore settingsStore, IFolderPicker folderPicker)
    {
        _fileSystem = fileSystem;
        _settingsStore = settingsStore;
        _folderPicker = folderPicker;

        Padding = new Thickness(24);

        _currentProjectLabel = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 16)
        };

        var openProjectButton = new Button
        {
            Content = "Open Project...",
            HorizontalAlignment = HorizontalAlignment.Left,
            MinHeight = 44
        };
        openProjectButton.Click += (_, _) => OpenProject();

        _recentProjectsList = new StackPanel
        {
            Spacing = 8,
            Margin = new Thickness(0, 16, 0, 0)
        };

        _resultLabel = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 0)
        };

        Child = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                new TextBlock
                {
                    Text = "Recent Projects",
                    FontSize = 20,
                    FontWeight = FontWeights.SemiBold
                },
                _currentProjectLabel,
                openProjectButton,
                _resultLabel,
                new TextBlock
                {
                    Text = "Recent",
                    FontSize = 16,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 16, 0, 0)
                },
                _recentProjectsList
            }
        };

        Refresh();
    }

    public void Refresh()
    {
        var currentProjectFolder = ProjectFolderService.GetCurrentProjectFolder(_fileSystem, _settingsStore);
        var currentProjectName = ProjectFolderService.GetCurrentProjectName(_fileSystem, _settingsStore);

        _currentProjectLabel.Text = currentProjectFolder is null
            ? "No project open."
            : $"Current project: {currentProjectName} ({currentProjectFolder})";

        _recentProjectsList.Children.Clear();
        var recentProjects = RecentProjectsService.GetRecentProjects(_fileSystem, _settingsStore);

        if (recentProjects.Length == 0)
        {
            _recentProjectsList.Children.Add(new TextBlock { Text = "No recent projects yet." });
            return;
        }

        foreach (var entry in recentProjects)
        {
            _recentProjectsList.Children.Add(BuildEntryRow(entry));
        }
    }

    private UIElement BuildEntryRow(RecentProjectsService.Entry entry)
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12
        };

        var label = new TextBlock
        {
            Text = entry.IsMissing ? $"{entry.Name} ({entry.Folder}) — missing" : $"{entry.Name} ({entry.Folder})",
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 400
        };

        var openButton = new Button { Content = "Open" };
        openButton.Click += (_, _) => OpenRecentProject(entry.Folder);

        var removeButton = new Button { Content = "Remove" };
        removeButton.Click += (_, _) =>
        {
            RecentProjectsService.Remove(_settingsStore, entry.Folder);
            Refresh();
        };

        row.Children.Add(label);
        row.Children.Add(openButton);
        row.Children.Add(removeButton);
        return row;
    }

    private void OpenRecentProject(string folder)
    {
        ProjectFolderService.TryOpenProject(_fileSystem, _settingsStore, folder, out var message);
        _resultLabel.Text = message;
        Refresh();
    }

    private async void OpenProject()
    {
        var path = await _folderPicker.PickFolderAsync();
        if (path is null)
        {
            return;
        }

        ProjectFolderService.TryOpenProject(_fileSystem, _settingsStore, path, out var message);
        _resultLabel.Text = message;
        Refresh();
    }
}
