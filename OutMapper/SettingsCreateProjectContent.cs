using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;

namespace OutMapper;

public sealed class SettingsCreateProjectContent : Border
{
    private readonly TextBox _projectNameInput;
    private readonly TextBlock _resultLabel;

    public SettingsCreateProjectContent()
    {
        Padding = new Thickness(24);

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

    private void CreateProject()
    {
        var created = ProjectFolderService.TryCreateProject(_projectNameInput.Text, out var message);
        _resultLabel.Text = message;

        if (created)
        {
            _projectNameInput.Text = string.Empty;
        }
    }
}
