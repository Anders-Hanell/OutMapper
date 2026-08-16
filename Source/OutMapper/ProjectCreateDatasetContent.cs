using System;
using System.IO;
using System.Threading.Tasks;
using Messages;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace OutMapper;

public sealed class ProjectCreateDatasetContent : Border
{
    private readonly IFolderPicker _folderPicker;
    private readonly TextBox _datasetNameInput;
    private readonly TextBlock _selectedFolderLabel;
    private readonly TextBlock _resultLabel;
    private string? _currentProjectFolder;
    private string? _selectedRawDataFolderPath;

    public ProjectCreateDatasetContent() : this(new WindowsFolderPicker())
    {
    }

    internal ProjectCreateDatasetContent(IFolderPicker folderPicker)
    {
        _folderPicker = folderPicker;

        Padding = new Thickness(24);
        Background = GetThemeBrush("SystemControlBackgroundChromeMediumLowBrush");
        CornerRadius = new CornerRadius(12);

        _datasetNameInput = new TextBox
        {
            PlaceholderText = "Enter dataset name",
            MinWidth = 280,
            Margin = new Thickness(0, 0, 0, 12)
        };

        var selectFolderButton = new Button
        {
            Content = "Select raw data folder",
            HorizontalAlignment = HorizontalAlignment.Left,
            MinHeight = 44
        };

        selectFolderButton.Click += (_, _) => SelectRawDataFolder();

        _selectedFolderLabel = new TextBlock
        {
            Text = "No raw data folder selected.",
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
                selectFolderButton,
                _selectedFolderLabel,
                createButton,
                _resultLabel
            }
        };
    }

    public void SetProject(string? projectName)
    {
        _currentProjectFolder = projectName;
        _datasetNameInput.Text = string.Empty;
        _resultLabel.Text = string.Empty;
        _selectedRawDataFolderPath = null;
        _selectedFolderLabel.Text = "No raw data folder selected.";
    }

    private async void SelectRawDataFolder()
    {
        var path = await _folderPicker.PickFolderAsync();
        if (path is null)
        {
            return;
        }

        _selectedRawDataFolderPath = path;

        var csvFileCount = Directory.GetFiles(path, "*.csv", SearchOption.TopDirectoryOnly).Length;
        _selectedFolderLabel.Text = $"{path} ({csvFileCount} CSV file(s) found)";
    }

    private void CreateDataset()
    {
        if (_currentProjectFolder is null)
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
        MessageRouter.SendMessage(new CreateDatasetRequest(datasetName, _currentProjectFolder, _selectedRawDataFolderPath));
    }

    internal void OnCreateDatasetResponseReceived(CreateDatasetResponse response)
    {
        // GatewayToTaskManager guarantees that incoming messages are dispatched on the UI thread.
        if (!string.Equals(response.ProjectFolder, _currentProjectFolder, StringComparison.Ordinal))
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
