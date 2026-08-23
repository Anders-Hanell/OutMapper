using System.Collections.Immutable;
using System.Runtime.InteropServices.WindowsRuntime;
using DataStructures;
using Messages;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;

namespace OutMapper;

public sealed class ProjectFigureSelectGraphsContent : Border
{
    private const string NoneOptionText = "(none)";
    private const string NoLabelsOptionText = "No labels";
    private const string UppercaseLabelsOptionText = "Uppercase (A, B, C...)";
    private const string LowercaseLabelsOptionText = "Lowercase (a, b, c...)";

    internal static ProjectFigureSelectGraphsContent? Current { get; private set; }

    private const int PreviewMaxDimensionPx = 900;

    private readonly IFolderOpener _folderOpener;
    private readonly Grid _cellsGrid;
    private readonly TextBlock _noSizeLabel;
    private readonly ComboBox _labelStyleComboBox;
    private readonly TextBlock _statusLabel;
    private readonly Image _previewImage;
    private readonly TextBlock _previewPlaceholder;
    private readonly DispatcherTimer _previewDebounceTimer;
    private string? _currentProjectFolder;
    private string? _currentFigureName;
    private int _rowCount;
    private int _colCount;
    private string?[] _cellAnalysisNames = [];
    private FigureLabelStyle _labelStyle = FigureLabelStyle.None;
    private ImmutableArray<string> _availableAnalysisNames = ImmutableArray<string>.Empty;

    public ProjectFigureSelectGraphsContent() : this(new DesktopFolderOpener())
    {
    }

    internal ProjectFigureSelectGraphsContent(IFolderOpener folderOpener)
    {
        _folderOpener = folderOpener;

        Padding = new Thickness(24);
        Background = GetThemeBrush("SystemControlBackgroundChromeMediumLowBrush");
        CornerRadius = new CornerRadius(12);

        _cellsGrid = new Grid { Margin = new Thickness(0, 0, 0, 12) };

        _noSizeLabel = new TextBlock
        {
            Text = "Set a size for this figure first.",
            Visibility = Visibility.Collapsed
        };

        _labelStyleComboBox = new ComboBox
        {
            Header = "Graph labels",
            MinWidth = 220,
            Items = { NoLabelsOptionText, UppercaseLabelsOptionText, LowercaseLabelsOptionText },
            SelectedIndex = 0
        };

        _labelStyleComboBox.SelectionChanged += (_, _) =>
        {
            _labelStyle = _labelStyleComboBox.SelectedItem switch
            {
                UppercaseLabelsOptionText => FigureLabelStyle.Uppercase,
                LowercaseLabelsOptionText => FigureLabelStyle.Lowercase,
                _ => FigureLabelStyle.None
            };
            SchedulePreviewUpdate();
        };

        var createButton = new Button
        {
            Content = "Create",
            HorizontalAlignment = HorizontalAlignment.Left,
            MinHeight = 44,
            MinWidth = 120
        };

        createButton.Click += (_, _) => CreateFigureGraph();

        var openOutputFolderButton = new Button
        {
            Content = "Open output folder",
            HorizontalAlignment = HorizontalAlignment.Left,
            MinHeight = 44,
            MinWidth = 120
        };

        openOutputFolderButton.Click += (_, _) => OpenOutputFolder();

        _statusLabel = new TextBlock
        {
            Text = string.Empty,
            Margin = new Thickness(0, 8, 0, 0),
            TextWrapping = TextWrapping.Wrap,
            Foreground = GetThemeBrush("SystemControlForegroundAccentBrush")
        };

        _previewImage = new Image
        {
            Stretch = Stretch.Uniform,
            Visibility = Visibility.Collapsed
        };

        _previewPlaceholder = new TextBlock
        {
            Text = "Set a size for this figure first.",
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = GetThemeBrush("SystemControlForegroundBaseMediumBrush")
        };

        var previewPane = new Border
        {
            MinWidth = 280,
            Padding = new Thickness(16),
            Margin = new Thickness(24, 0, 0, 0),
            CornerRadius = new CornerRadius(8),
            Background = GetThemeBrush("SystemControlBackgroundChromeMediumBrush"),
            Child = new Grid { Children = { _previewPlaceholder, _previewImage } }
        };

        _previewDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
        _previewDebounceTimer.Tick += (_, _) =>
        {
            _previewDebounceTimer.Stop();
            UpdatePreview();
        };

        var settingsColumn = new StackPanel
        {
            Spacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Select graphs",
                    FontSize = 18,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 8)
                },
                _noSizeLabel,
                new ScrollViewer
                {
                    HorizontalScrollMode = ScrollMode.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Content = _cellsGrid
                },
                _labelStyleComboBox,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 12,
                    Children = { createButton, openOutputFolderButton }
                },
                _statusLabel
            }
        };

        var contentGrid = new Grid
        {
            ColumnDefinitions =
            {
                // Fixed rather than Auto: an Auto column is measured at infinite width, so the status
                // label's wrapping text (e.g. a long "Saved to <path>" message) would never actually wrap
                // and would inflate this column, squeezing the preview pane out of the available space.
                new ColumnDefinition { Width = new GridLength(340) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            }
        };
        Grid.SetColumn(settingsColumn, 0);
        Grid.SetColumn(previewPane, 1);
        contentGrid.Children.Add(settingsColumn);
        contentGrid.Children.Add(previewPane);

        Child = contentGrid;

        Current = this;
    }

    public void SetFigure(string projectName, string figureName)
    {
        _currentProjectFolder = projectName;
        _currentFigureName = figureName;
        _statusLabel.Text = string.Empty;

        MessageRouter.SendMessage(new FigureLayoutRequest(projectName, figureName));
        MessageRouter.SendMessage(new AnalysesWithGraphListRequest(projectName));
    }

    internal void OnFigureLayoutResponseReceived(FigureLayoutResponse response)
    {
        _rowCount = response.RowCount;
        _colCount = response.ColCount;
        _cellAnalysisNames = response.CellAnalysisNames.ToArray();
        SetLabelStyle(response.LabelStyle);
        RebuildGrid();
    }

    internal void OnSaveFigureSizeResponseReceived(SaveFigureSizeResponse response)
    {
        if (!response.Success)
        {
            return;
        }

        _rowCount = response.RowCount;
        _colCount = response.ColCount;
        _cellAnalysisNames = response.CellAnalysisNames.ToArray();
        SetLabelStyle(response.LabelStyle);
        RebuildGrid();
    }

    private void SetLabelStyle(FigureLabelStyle labelStyle)
    {
        _labelStyle = labelStyle;
        _labelStyleComboBox.SelectedItem = labelStyle switch
        {
            FigureLabelStyle.Uppercase => UppercaseLabelsOptionText,
            FigureLabelStyle.Lowercase => LowercaseLabelsOptionText,
            _ => NoLabelsOptionText
        };
    }

    internal void OnAnalysesWithGraphListResponseReceived(AnalysesWithGraphListResponse response)
    {
        // GatewayToTaskManager guarantees that incoming messages are dispatched on the UI thread.
        if (!string.Equals(response.ProjectFolder, _currentProjectFolder, StringComparison.Ordinal))
        {
            return;
        }

        _availableAnalysisNames = response.AnalysisNames;
        RebuildGrid();
    }

    private void RebuildGrid()
    {
        _cellsGrid.Children.Clear();
        _cellsGrid.RowDefinitions.Clear();
        _cellsGrid.ColumnDefinitions.Clear();

        if (_rowCount <= 0 || _colCount <= 0)
        {
            _noSizeLabel.Visibility = Visibility.Visible;
            SchedulePreviewUpdate();
            return;
        }

        _noSizeLabel.Visibility = Visibility.Collapsed;

        for (var row = 0; row < _rowCount; row++)
        {
            _cellsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        for (var col = 0; col < _colCount; col++)
        {
            _cellsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        }

        for (var row = 0; row < _rowCount; row++)
        {
            for (var col = 0; col < _colCount; col++)
            {
                var cellIndex = row * _colCount + col;
                var currentValue = cellIndex < _cellAnalysisNames.Length ? _cellAnalysisNames[cellIndex] : null;

                var comboBox = new ComboBox
                {
                    Header = $"Row {row + 1}, Col {col + 1}",
                    MinWidth = 200,
                    Margin = new Thickness(4)
                };

                comboBox.Items.Add(NoneOptionText);
                foreach (var analysisName in _availableAnalysisNames)
                {
                    comboBox.Items.Add(analysisName);
                }

                comboBox.SelectedItem = currentValue is not null && _availableAnalysisNames.Contains(currentValue)
                    ? currentValue
                    : NoneOptionText;

                var capturedRow = row;
                var capturedCol = col;
                comboBox.SelectionChanged += (_, _) =>
                {
                    var selected = comboBox.SelectedItem as string;
                    var index = capturedRow * _colCount + capturedCol;
                    if (index < _cellAnalysisNames.Length)
                    {
                        _cellAnalysisNames[index] = selected == NoneOptionText ? null : selected;
                    }

                    SchedulePreviewUpdate();
                };

                Grid.SetRow(comboBox, row);
                Grid.SetColumn(comboBox, col);
                _cellsGrid.Children.Add(comboBox);
            }
        }

        SchedulePreviewUpdate();
    }

    private void SchedulePreviewUpdate()
    {
        _previewDebounceTimer.Stop();
        _previewDebounceTimer.Start();
    }

    private async void UpdatePreview()
    {
        if (_currentProjectFolder is null || _rowCount <= 0 || _colCount <= 0)
        {
            ShowPreviewPlaceholder("Set a size for this figure first.");
            return;
        }

        var buildRequest = new BuildFigureDrawDataRequest(
            Guid.NewGuid(), _currentProjectFolder, _rowCount, _colCount,
            _cellAnalysisNames.ToImmutableArray(), _labelStyle);
        var buildResponse = await GatewayRequestCorrelator
            .SendAsync<BuildFigureDrawDataRequest, BuildFigureDrawDataResponse>(buildRequest);
        var buildResult = buildResponse.Result;

        if (buildResult is not Success<FigureDrawData> success)
        {
            ShowPreviewPlaceholder("Select at least one graph to preview the figure.");
            return;
        }

        var pngBytes = await FigurePreviewRenderer.RenderPngAsync(success.Value, PreviewMaxDimensionPx);
        if (pngBytes is null)
        {
            ShowPreviewPlaceholder("Could not render a preview.");
            return;
        }

        var bitmap = new BitmapImage();
        using (var stream = new InMemoryRandomAccessStream())
        {
            await stream.WriteAsync(pngBytes.AsBuffer());
            stream.Seek(0);
            await bitmap.SetSourceAsync(stream);
        }

        _previewImage.Source = bitmap;
        _previewImage.Visibility = Visibility.Visible;
        _previewPlaceholder.Visibility = Visibility.Collapsed;
    }

    private void ShowPreviewPlaceholder(string message)
    {
        _previewPlaceholder.Text = message;
        _previewPlaceholder.Visibility = Visibility.Visible;
        _previewImage.Visibility = Visibility.Collapsed;
    }

    private void CreateFigureGraph()
    {
        if (_currentProjectFolder is null || _currentFigureName is null)
        {
            _statusLabel.Text = "No figure selected.";
            return;
        }

        if (_rowCount <= 0 || _colCount <= 0)
        {
            _statusLabel.Text = "Set a size for this figure first.";
            return;
        }

        _statusLabel.Text = "Creating figure...";
        MessageRouter.SendMessage(new CreateFigureGraphRequest(
            _currentProjectFolder, _currentFigureName, _rowCount, _colCount, _cellAnalysisNames.ToImmutableArray(),
            _labelStyle));
    }

    private async void OpenOutputFolder()
    {
        if (_currentProjectFolder is null)
        {
            _statusLabel.Text = "No figure selected.";
            return;
        }

        var outputFolder = System.IO.Path.Combine(_currentProjectFolder, ProjectFolderService.ProjectOutputFolderName);
        var opened = await _folderOpener.OpenFolderAsync(outputFolder);
        if (!opened)
        {
            _statusLabel.Text = "Could not open the output folder.";
        }
    }

    internal async void OnCreateFigureGraphResponseReceived(CreateFigureGraphResponse response)
    {
        if (!response.Success)
        {
            _statusLabel.Text = $"Could not create figure: {response.ErrorMessage}";
            return;
        }

        var outputPath = await FigureGraphPdfService.GeneratePdfAsync(response.ProjectFolder, response.FigureName, response);

        _statusLabel.Text = outputPath is null
            ? "Generated, but the PDF could not be written."
            : $"Saved to {outputPath}";
    }

    private static Brush? GetThemeBrush(string resourceKey)
    {
        return Application.Current.Resources.TryGetValue(resourceKey, out var resource)
            ? resource as Brush
            : null;
    }
}
