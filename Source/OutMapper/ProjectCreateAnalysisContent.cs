using Messages;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace OutMapper;

public sealed class ProjectCreateAnalysisContent : Border
{
    private readonly TextBox _analysisNameInput;
    private readonly TextBlock _resultLabel;
    private string? _currentProjectFolder;

    public ProjectCreateAnalysisContent()
    {
        Padding = new Thickness(24);
        Background = GetThemeBrush("SystemControlBackgroundChromeMediumLowBrush");
        CornerRadius = new CornerRadius(12);

        _analysisNameInput = new TextBox
        {
            PlaceholderText = "Enter analysis name",
            MinWidth = 280,
            Margin = new Thickness(0, 0, 0, 12)
        };

        var createButton = new Button
        {
            Content = "Create",
            HorizontalAlignment = HorizontalAlignment.Left,
            MinHeight = 44,
            MinWidth = 120
        };

        createButton.Click += (_, _) => CreateAnalysis();

        _resultLabel = new TextBlock
        {
            Text = string.Empty,
            Margin = new Thickness(0, 8, 0, 0),
            Foreground = GetThemeBrush("SystemControlForegroundAccentBrush")
        };

        Child = new StackPanel
        {
            Spacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Create a new analysis",
                    FontSize = 18,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 8)
                },
                _analysisNameInput,
                createButton,
                _resultLabel
            }
        };
    }

    public void SetProject(string? projectName)
    {
        _currentProjectFolder = projectName;
        _analysisNameInput.Text = string.Empty;
        _resultLabel.Text = string.Empty;
    }

    private void CreateAnalysis()
    {
        if (_currentProjectFolder is null)
        {
            _resultLabel.Text = "No project selected.";
            return;
        }

        var analysisName = _analysisNameInput.Text?.Trim();
        if (string.IsNullOrWhiteSpace(analysisName))
        {
            _resultLabel.Text = "Please enter an analysis name.";
            return;
        }

        _resultLabel.Text = $"Creating '{analysisName}'...";
        MessageRouter.SendMessage(new CreateAnalysisRequest(analysisName, _currentProjectFolder));
    }

    internal void OnCreateAnalysisResponseReceived(CreateAnalysisResponse response)
    {
        // GatewayToTaskManager guarantees that incoming messages are dispatched on the UI thread.
        if (!string.Equals(response.ProjectFolder, _currentProjectFolder, StringComparison.Ordinal))
        {
            return;
        }

        _resultLabel.Text = response.Success
            ? $"Created '{response.AnalysisName}'"
            : $"Failed to create '{response.AnalysisName}'";
    }

    private static Brush? GetThemeBrush(string resourceKey)
    {
        return Application.Current.Resources.TryGetValue(resourceKey, out var resource)
            ? resource as Brush
            : null;
    }
}
