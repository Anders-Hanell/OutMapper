using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;

namespace OutMapper;

public sealed class SettingsCreateProjectContent : Border
{
    private readonly IFolderPicker _folderPicker;
    private readonly TextBlock _selectedLocationLabel;
    private readonly TextBox _projectNameInput;
    private readonly TextBlock _resultLabel;
    private string? _selectedParentFolder;

    public SettingsCreateProjectContent() : this(new WindowsFolderPicker())
    {
    }

    internal SettingsCreateProjectContent(IFolderPicker folderPicker)
    {
        _folderPicker = folderPicker;

        Padding = new Thickness(24);

        var selectLocationButton = new Button
        {
            Content = "Select Location...",
            MinWidth = 120,
            MinHeight = 44,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        AutomationProperties.SetName(selectLocationButton, "Select location");
        selectLocationButton.Click += (_, _) => SelectLocation();

        _selectedLocationLabel = new TextBlock
        {
            Text = "No location selected.",
            TextWrapping = TextWrapping.Wrap
        };

        _projectNameInput = new TextBox
        {
            Header = "Project name",
            PlaceholderText = "Enter project name",
            MinWidth = 280,
            MaxWidth = 480,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        AutomationProperties.SetName(_projectNameInput, "Project name");

        var createButton = new Button
        {
            Content = "Create",
            MinWidth = 120,
            MinHeight = 44,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        AutomationProperties.SetName(createButton, "Create project");
        createButton.Click += (_, _) => CreateProject();

        _resultLabel = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap
        };
        AutomationProperties.SetLiveSetting(_resultLabel, AutomationLiveSetting.Polite);

        Child = new StackPanel
        {
            Spacing = 16,
            Children =
            {
                new TextBlock
                {
                    Text = "Create Project",
                    FontSize = 20,
                    FontWeight = FontWeights.SemiBold
                },
                selectLocationButton,
                _selectedLocationLabel,
                _projectNameInput,
                createButton,
                _resultLabel
            }
        };
    }

    public void Reset()
    {
        _resultLabel.Text = string.Empty;
    }

    private async void SelectLocation()
    {
        var path = await _folderPicker.PickFolderAsync();
        if (path is null)
        {
            return;
        }

        _selectedParentFolder = path;
        _selectedLocationLabel.Text = path;
    }

    private void CreateProject()
    {
        var created = ProjectFolderService.TryCreateProject(_selectedParentFolder, _projectNameInput.Text, out var message);
        _resultLabel.Text = message;

        if (created)
        {
            _projectNameInput.Text = string.Empty;
        }
    }
}
