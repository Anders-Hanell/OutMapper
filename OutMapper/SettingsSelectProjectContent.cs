using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;

namespace OutMapper;

public sealed class SettingsSelectProjectContent : Border
{
    private readonly ComboBox _projectSelector;
    private readonly TextBlock _resultLabel;

    public SettingsSelectProjectContent()
    {
        Padding = new Thickness(24);

        _projectSelector = new ComboBox
        {
            Header = "Project",
            PlaceholderText = "Choose a project",
            MinWidth = 280,
            MaxWidth = 480,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        AutomationProperties.SetName(_projectSelector, "Project");

        var selectButton = new Button
        {
            Content = "Select",
            MinWidth = 120,
            MinHeight = 44,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        AutomationProperties.SetName(selectButton, "Select project");
        selectButton.Click += (_, _) => SelectProject();

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
                    Text = "Select Project",
                    FontSize = 20,
                    FontWeight = FontWeights.SemiBold
                },
                _projectSelector,
                selectButton,
                _resultLabel
            }
        };
    }

    public void Refresh()
    {
        _projectSelector.Items.Clear();
        _projectSelector.SelectedItem = null;

        var projectNames = ProjectFolderService.GetProjectNames(out var error);
        if (error is not null)
        {
            _projectSelector.IsEnabled = false;
            _resultLabel.Text = error;
            return;
        }

        foreach (var projectName in projectNames)
        {
            _projectSelector.Items.Add(projectName);
        }

        _projectSelector.IsEnabled = projectNames.Length > 0;
        if (projectNames.Length == 0)
        {
            _resultLabel.Text = "No projects found.";
            return;
        }

        var selectedProject = ProjectFolderService.GetSelectedProjectName(out var selectionError);
        if (selectedProject is not null)
        {
            _projectSelector.SelectedItem = selectedProject;
            _resultLabel.Text = $"Current project: {selectedProject}";
        }
        else
        {
            _resultLabel.Text = selectionError ?? "Choose a project from the list.";
        }
    }

    private void SelectProject()
    {
        ProjectFolderService.TrySelectProject(_projectSelector.SelectedItem as string, out var message);
        _resultLabel.Text = message;
    }
}
