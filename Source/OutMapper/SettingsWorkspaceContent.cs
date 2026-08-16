using Messages;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;

namespace OutMapper;

public sealed class SettingsWorkspaceContent : Border
{
    private const string WorkspaceFolderPathKey = "WorkspaceFolderPath";
    private readonly ISettingsStore _settingsStore;
    private readonly IFolderPicker _folderPicker;
    private readonly TextBlock _currentWorkspaceValue;

    public SettingsWorkspaceContent() : this(LocalSettingsStore.Instance, new WindowsFolderPicker())
    {
    }

    internal SettingsWorkspaceContent(ISettingsStore settingsStore, IFolderPicker folderPicker)
    {
        _settingsStore = settingsStore;
        _folderPicker = folderPicker;

        Padding = new Thickness(16);

        var title = new TextBlock
        {
            Text = "Current Workspace",
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 8)
        };

        _currentWorkspaceValue = new TextBlock
        {
            Text = _settingsStore.GetString(WorkspaceFolderPathKey) ?? "Not set",
            Margin = new Thickness(0, 0, 0, 16),
            FontSize = 16
        };

        var selectWorkspaceButton = new Button
        {
            Content = "Select Workspace",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        selectWorkspaceButton.Click += SelectWorkspaceButton_Click;

        Child = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Children =
            {
                title,
                _currentWorkspaceValue,
                selectWorkspaceButton
            }
        };
    }

    private async void SelectWorkspaceButton_Click(object sender, RoutedEventArgs e)
    {
        var path = await _folderPicker.PickFolderAsync();
        if (path is not null)
        {
            var previousPath = _settingsStore.GetString(WorkspaceFolderPathKey);
            _settingsStore.SetString(WorkspaceFolderPathKey, path);
            if (!string.Equals(previousPath, path, StringComparison.Ordinal))
            {
                ProjectFolderService.ClearSelectedProject();
            }
            _currentWorkspaceValue.Text = path;
            MessageRouter.SendMessage(new WorkspaceChanged(path));
        }
    }

    public static string? LoadWorkspaceFolderPath() => LocalSettingsStore.Instance.GetString(WorkspaceFolderPathKey);
}
