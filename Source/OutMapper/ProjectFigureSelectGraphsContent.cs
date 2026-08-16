using System.Collections.Immutable;
using Messages;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace OutMapper;

public sealed class ProjectFigureSelectGraphsContent : Border
{
    private const string NoneOptionText = "(none)";

    internal static ProjectFigureSelectGraphsContent? Current { get; private set; }

    private readonly Grid _cellsGrid;
    private readonly TextBlock _noSizeLabel;
    private readonly TextBlock _statusLabel;
    private string? _currentProjectFolder;
    private string? _currentFigureName;
    private int _rowCount;
    private int _colCount;
    private string?[] _cellAnalysisNames = [];
    private ImmutableArray<string> _availableAnalysisNames = ImmutableArray<string>.Empty;

    public ProjectFigureSelectGraphsContent()
    {
        Padding = new Thickness(24);
        Background = GetThemeBrush("SystemControlBackgroundChromeMediumLowBrush");
        CornerRadius = new CornerRadius(12);

        _cellsGrid = new Grid { Margin = new Thickness(0, 0, 0, 12) };

        _noSizeLabel = new TextBlock
        {
            Text = "Set a size for this figure first.",
            Visibility = Visibility.Collapsed
        };

        var createButton = new Button
        {
            Content = "Create",
            HorizontalAlignment = HorizontalAlignment.Left,
            MinHeight = 44,
            MinWidth = 120
        };

        createButton.Click += (_, _) => CreateFigureGraph();

        _statusLabel = new TextBlock
        {
            Text = string.Empty,
            Margin = new Thickness(0, 8, 0, 0),
            TextWrapping = TextWrapping.Wrap,
            Foreground = GetThemeBrush("SystemControlForegroundAccentBrush")
        };

        Child = new StackPanel
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
                createButton,
                _statusLabel
            }
        };

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
        RebuildGrid();
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
                };

                Grid.SetRow(comboBox, row);
                Grid.SetColumn(comboBox, col);
                _cellsGrid.Children.Add(comboBox);
            }
        }
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
            _currentProjectFolder, _currentFigureName, _rowCount, _colCount, _cellAnalysisNames.ToImmutableArray()));
    }

    internal void OnCreateFigureGraphResponseReceived(CreateFigureGraphResponse response)
    {
        if (!response.Success)
        {
            _statusLabel.Text = $"Could not create figure: {response.ErrorMessage}";
            return;
        }

        var outputPath = FigureGraphPdfService.GeneratePdf(response.ProjectFolder, response.FigureName, response);

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
