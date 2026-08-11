using System;
using Messages;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace OutMapper;

public sealed class DatasetsContent : Border
{
    private readonly TextBlock _contentLabel;
    private readonly ContentControl _contentArea;
    private TextBox? _datasetNameInput;
    private TextBlock? _createResultLabel;

    public DatasetsContent()
    {
        Padding = new Thickness(16);
        Background = GetThemeBrush("ApplicationPageBackgroundThemeBrush");
        CornerRadius = new CornerRadius(12);

        _contentLabel = new TextBlock
        {
            Text = "Current datasets panel",
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(16)
        };

        var navigationPanel = new StackPanel
        {
            Spacing = 12,
            Padding = new Thickness(16),
            Background = GetThemeBrush("SystemControlPageBackgroundChromeLowBrush"),
            CornerRadius = new CornerRadius(12),
            Children =
            {
                new TextBlock
                {
                    Text = "Datasets",
                    FontSize = 18,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 8)
                },
                CreateNavButton("Current datasets", OnCurrentDatasetsClicked),
                CreateNavButton("Create dataset", OnCreateDatasetClicked)
            }
        };

        _contentArea = new ContentControl
        {
            Content = _contentLabel
        };

        var mainGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(280, GridUnitType.Pixel) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            },
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto }
            },
            Children =
            {
                navigationPanel,
                new Border
                {
                    Padding = new Thickness(24),
                    Background = GetThemeBrush("SystemControlBackgroundChromeMediumLowBrush"),
                    CornerRadius = new CornerRadius(12),
                    Child = _contentArea,
                    Margin = new Thickness(24, 0, 0, 0)
                }
            }
        };

        Grid.SetColumn(navigationPanel, 0);
        Grid.SetColumn(mainGrid.Children[1], 1);

        Child = mainGrid;

        MessageRouter.ReceiveMessage((s, m) =>
        {
            switch (m)
            {
                case DatasetListResponse listResponse:
                    OnDatasetListResponseReceived(listResponse);
                    break;
                case CreateDatasetResponse createResponse:
                    OnCreateDatasetResponseReceived(createResponse);
                    break;
            }
        });
    }

    private void OnCurrentDatasetsClicked()
    {
        _contentArea.Content = _contentLabel;
        UpdateContent("Loading current datasets...");
        MessageRouter.SendMessage(new DatasetListRequest(WorkspaceFolder: null));
    }

    private void OnCreateDatasetClicked()
    {
        _contentArea.Content = BuildCreateDatasetPanel();
    }

    private StackPanel BuildCreateDatasetPanel()
    {
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

        _createResultLabel = new TextBlock
        {
            Text = string.Empty,
            Margin = new Thickness(0, 8, 0, 0),
            Foreground = GetThemeBrush("SystemControlForegroundAccentBrush")
        };

        return new StackPanel
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
                createButton
                ,_createResultLabel
            }
        };
    }

    private void CreateDataset()
    {
        var datasetName = _datasetNameInput?.Text?.Trim();
        if (string.IsNullOrWhiteSpace(datasetName))
        {
            if (_createResultLabel is not null)
            {
                _createResultLabel.Text = "Please enter a dataset name.";
            }
            return;
        }
        if (_createResultLabel is not null)
        {
            _createResultLabel.Text = $"Creating '{datasetName}'...";
        }
        MessageRouter.SendMessage(new CreateDatasetRequest(datasetName, WorkspaceFolder: null));
    }

    private Button CreateNavButton(string label, Action onClick)
    {
        var button = new Button
        {
            Content = label,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MinHeight = 44,
            Margin = new Thickness(0),
            FontSize = 14
        };

        button.Click += (_, _) => onClick();
        return button;
    }

    private void UpdateContent(string title)
    {
        _contentLabel.Text = title;
    }

    private void OnDatasetListResponseReceived(DatasetListResponse response)
    {
        // MessageRouter guarantees that incoming messages are processed on the UI thread.
        var datasetsText = response.DatasetNames.Length > 0
            ? string.Join("\n", response.DatasetNames)
            : "No datasets found.";

        _contentLabel.Text = $"Datasets:\n{datasetsText}";
    }

    private void OnCreateDatasetResponseReceived(CreateDatasetResponse response)
    {
        // MessageRouter guarantees that incoming messages are processed on the UI thread.
        if (_createResultLabel is not null)
        {
            _createResultLabel.Text = response.Success
                ? $"Created '{response.DatasetName}'"
                : $"Failed to create '{response.DatasetName}'";
        }
    }

    private static Brush? GetThemeBrush(string resourceKey)
    {
        return Application.Current.Resources.TryGetValue(resourceKey, out var resource)
            ? resource as Brush
            : null;
    }
}
