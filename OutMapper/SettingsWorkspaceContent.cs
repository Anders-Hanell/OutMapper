using Messages;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace OutMapper;

public sealed class SettingsWorkspaceContent : Border
{
    private const string WorkspaceFolderPathKey = "WorkspaceFolderPath";
    private readonly TextBlock _currentWorkspaceValue;

    public SettingsWorkspaceContent()
    {
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
            Text = LoadWorkspaceFolderPath() ?? "Not set",
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
        var folder = await PickWorkspaceFolderAsync();
        if (folder is not null)
        {
            var path = folder.Path;
            var previousPath = LoadWorkspaceFolderPath();
            SaveWorkspaceFolderPath(path);
            if (!string.Equals(previousPath, path, StringComparison.Ordinal))
            {
                ProjectFolderService.ClearSelectedProject();
            }
            _currentWorkspaceValue.Text = path;
            MessageRouter.SendMessage(new WorkspaceChanged(path));
        }
    }

    public static string? LoadWorkspaceFolderPath()
    {
        return ApplicationData.Current.LocalSettings.Values.TryGetValue(WorkspaceFolderPathKey, out var value)
            ? value as string
            : null;
    }

    private static void SaveWorkspaceFolderPath(string path)
    {
        ApplicationData.Current.LocalSettings.Values[WorkspaceFolderPathKey] = path;
    }

    private static async Task<StorageFolder?> PickWorkspaceFolderAsync()
    {
        var picker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.Desktop
        };
        picker.FileTypeFilter.Add("*");
        return await picker.PickSingleFolderAsync();
    }
}
