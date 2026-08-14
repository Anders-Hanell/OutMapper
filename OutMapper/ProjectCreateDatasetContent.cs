using System;
using System.IO;
using System.Threading.Tasks;
using Messages;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Storage.Pickers;

namespace OutMapper;

public sealed class ProjectCreateDatasetContent : Border
{
    private readonly TextBox _datasetNameInput;
    private readonly TextBlock _selectedFolderLabel;
    private readonly TextBlock _resultLabel;
    private string? _currentProjectName;
    private string? _selectedRawDataFolderPath;

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
        _currentProjectName = projectName;
        _datasetNameInput.Text = string.Empty;
        _resultLabel.Text = string.Empty;
        _selectedRawDataFolderPath = null;
        _selectedFolderLabel.Text = "No raw data folder selected.";
    }

    private async void SelectRawDataFolder()
    {
        var folder = await PickRawDataFolderAsync();
        if (folder is null)
        {
            return;
        }

        _selectedRawDataFolderPath = folder.Path;

        var csvFileCount = Directory.GetFiles(folder.Path, "*.csv", SearchOption.TopDirectoryOnly).Length;
        _selectedFolderLabel.Text = $"{folder.Path} ({csvFileCount} CSV file(s) found)";
    }

    private static async Task<Windows.Storage.StorageFolder?> PickRawDataFolderAsync()
    {
        var picker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.Desktop
        };
        picker.FileTypeFilter.Add("*");
        return await picker.PickSingleFolderAsync();
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
        MessageRouter.SendMessage(new CreateDatasetRequest(datasetName, _currentProjectName, _selectedRawDataFolderPath));
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
