using Microsoft.UI.Text;

namespace OutMapper;

public sealed class SettingsProjectsContent : Border
{
    private readonly StackPanel _projectList;
    private readonly TextBlock _statusLabel;

    public SettingsProjectsContent()
    {
        Padding = new Thickness(24);

        _projectList = new StackPanel
        {
            Spacing = 8
        };

        _statusLabel = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap
        };

        Child = new StackPanel
        {
            Spacing = 16,
            Children =
            {
                new TextBlock
                {
                    Text = "Current Projects",
                    FontSize = 20,
                    FontWeight = FontWeights.SemiBold
                },
                _statusLabel,
                _projectList
            }
        };
    }

    public void Refresh()
    {
        _projectList.Children.Clear();
        var projectNames = ProjectFolderService.GetProjectNames(out var error);

        if (error is not null)
        {
            _statusLabel.Text = error;
            return;
        }

        if (projectNames.Length == 0)
        {
            _statusLabel.Text = "No projects found.";
            return;
        }

        _statusLabel.Text = $"{projectNames.Length} project{(projectNames.Length == 1 ? string.Empty : "s")} found:";
        foreach (var projectName in projectNames)
        {
            _projectList.Children.Add(new TextBlock
            {
                Text = projectName,
                TextWrapping = TextWrapping.Wrap
            });
        }
    }
}
