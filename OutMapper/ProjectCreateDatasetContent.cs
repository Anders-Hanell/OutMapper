using System;
using Messages;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace OutMapper;

public sealed class ProjectCreateDatasetContent : Border
{
    private readonly TextBox _datasetNameInput;
    private readonly TextBlock _resultLabel;
    private string? _currentProjectName;

    public ProjectCreateDatasetContent()
    {
        Padding = new Thickness(24);
        Background = GetThemeBrush("SystemControlBackgroundChromeMediumLowBrush");
        CornerRadius = new CornerRadius(12);

        _datasetNameInput = new TextBox
        {
            PlaceholderText = "Enter dataset name",
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

        createButton.Click += (_, _) => CreateDataset();

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
                    Text = "Create a new dataset",
                    FontSize = 18,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 8)
                },
                _datasetNameInput,
                createButton,
                _resultLabel
            }
        };
    }

    public void SetProject(string? projectName)
    {
        _currentProjectName = projectName;
        _datasetNameInput.Text = string.Empty;
        _resultLabel.Text = string.Empty;
    }

    private void CreateDataset()
    {
        if (_currentProjectName is null)
        {
            _resultLabel.Text = "No project selected.";
            return;
        }

        var datasetName = _datasetNameInput.Text?.Trim();
        if (string.IsNullOrWhiteSpace(datasetName))
        {
            _resultLabel.Text = "Please enter a dataset name.";
            return;
        }

        _resultLabel.Text = $"Creating '{datasetName}'...";
        MessageRouter.SendMessage(new CreateDatasetRequest(datasetName, _currentProjectName));
    }

    internal void OnCreateDatasetResponseReceived(CreateDatasetResponse response)
    {
        // GatewayToTaskManager guarantees that incoming messages are dispatched on the UI thread.
        if (!string.Equals(response.ProjectName, _currentProjectName, StringComparison.Ordinal))
        {
            return;
        }

        _resultLabel.Text = response.Success
            ? $"Created '{response.DatasetName}'"
            : $"Failed to create '{response.DatasetName}'";
    }

    private static Brush? GetThemeBrush(string resourceKey)
    {
        return Application.Current.Resources.TryGetValue(resourceKey, out var resource)
            ? resource as Brush
            : null;
    }
}
