using Messages;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace OutMapper;

public sealed class ProjectCreateFigureContent : Border
{
    private readonly TextBox _figureNameInput;
    private readonly TextBlock _resultLabel;
    private string? _currentProjectFolder;

    public ProjectCreateFigureContent()
    {
        Padding = new Thickness(24);
        Background = GetThemeBrush("SystemControlBackgroundChromeMediumLowBrush");
        CornerRadius = new CornerRadius(12);

        _figureNameInput = new TextBox
        {
            PlaceholderText = "Enter figure name",
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

        createButton.Click += (_, _) => CreateFigure();

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
                    Text = "Create a new figure",
                    FontSize = 18,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 8)
                },
                _figureNameInput,
                createButton,
                _resultLabel
            }
        };
    }

    public void SetProject(string? projectName)
    {
        _currentProjectFolder = projectName;
        _figureNameInput.Text = string.Empty;
        _resultLabel.Text = string.Empty;
    }

    private void CreateFigure()
    {
        if (_currentProjectFolder is null)
        {
            _resultLabel.Text = "No project selected.";
            return;
        }

        var figureName = _figureNameInput.Text?.Trim();
        if (string.IsNullOrWhiteSpace(figureName))
        {
            _resultLabel.Text = "Please enter a figure name.";
            return;
        }

        _resultLabel.Text = $"Creating '{figureName}'...";
        MessageRouter.SendMessage(new CreateFigureRequest(figureName, _currentProjectFolder));
    }

    internal void OnCreateFigureResponseReceived(CreateFigureResponse response)
    {
        // GatewayToTaskManager guarantees that incoming messages are dispatched on the UI thread.
        if (!string.Equals(response.ProjectFolder, _currentProjectFolder, StringComparison.Ordinal))
        {
            return;
        }

        _resultLabel.Text = response.Success
            ? $"Created '{response.FigureName}'"
            : $"Failed to create '{response.FigureName}'";
    }

    private static Brush? GetThemeBrush(string resourceKey)
    {
        return Application.Current.Resources.TryGetValue(resourceKey, out var resource)
            ? resource as Brush
            : null;
    }
}
