using Messages;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace OutMapper;

public sealed class ProjectFigureSizeContent : Border
{
    private readonly TextBox _rowCountInput;
    private readonly TextBox _colCountInput;
    private readonly TextBlock _statusLabel;
    private string? _currentProjectFolder;
    private string? _currentFigureName;

    public ProjectFigureSizeContent()
    {
        Padding = new Thickness(24);
        Background = GetThemeBrush("SystemControlBackgroundChromeMediumLowBrush");
        CornerRadius = new CornerRadius(12);

        _rowCountInput = new TextBox
        {
            Header = "Rows",
            PlaceholderText = "e.g. 2",
            MinWidth = 280
        };

        _colCountInput = new TextBox
        {
            Header = "Columns",
            PlaceholderText = "e.g. 2",
            MinWidth = 280
        };

        var saveButton = new Button
        {
            Content = "Save size",
            HorizontalAlignment = HorizontalAlignment.Left,
            MinHeight = 44,
            MinWidth = 160
        };

        saveButton.Click += (_, _) => SaveSize();

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
                    Text = "Figure size",
                    FontSize = 18,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 8)
                },
                _rowCountInput,
                _colCountInput,
                saveButton,
                _statusLabel
            }
        };
    }

    public void SetFigure(string projectName, string figureName)
    {
        _currentProjectFolder = projectName;
        _currentFigureName = figureName;
        _statusLabel.Text = string.Empty;
        _rowCountInput.Text = string.Empty;
        _colCountInput.Text = string.Empty;

        MessageRouter.SendMessage(new FigureLayoutRequest(projectName, figureName));
    }

    internal void OnFigureLayoutResponseReceived(FigureLayoutResponse response)
    {
        if (!response.LayoutExists)
        {
            return;
        }

        _rowCountInput.Text = response.RowCount.ToString();
        _colCountInput.Text = response.ColCount.ToString();
    }

    private void SaveSize()
    {
        if (_currentProjectFolder is null || _currentFigureName is null)
        {
            _statusLabel.Text = "No figure selected.";
            return;
        }

        if (!int.TryParse(_rowCountInput.Text, out var rowCount) || rowCount <= 0)
        {
            _statusLabel.Text = "Enter a valid number of rows.";
            return;
        }

        if (!int.TryParse(_colCountInput.Text, out var colCount) || colCount <= 0)
        {
            _statusLabel.Text = "Enter a valid number of columns.";
            return;
        }

        _statusLabel.Text = "Saving size...";
        MessageRouter.SendMessage(new SaveFigureSizeRequest(_currentProjectFolder, _currentFigureName, rowCount, colCount));
    }

    internal void OnSaveFigureSizeResponseReceived(SaveFigureSizeResponse response)
    {
        if (!response.Success)
        {
            _statusLabel.Text = $"Could not save size: {response.ErrorMessage}";
            return;
        }

        _rowCountInput.Text = response.RowCount.ToString();
        _colCountInput.Text = response.ColCount.ToString();
        _statusLabel.Text = "Saved.";
    }

    private static Brush? GetThemeBrush(string resourceKey)
    {
        return Application.Current.Resources.TryGetValue(resourceKey, out var resource)
            ? resource as Brush
            : null;
    }
}
