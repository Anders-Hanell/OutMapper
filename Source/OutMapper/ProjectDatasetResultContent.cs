using Messages;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace OutMapper;

public sealed class ProjectDatasetResultContent : Border
{
    private readonly TextBlock _summaryLabel;
    private readonly StackPanel _fileOutcomesPanel;
    private string? _currentProjectName;
    private string? _currentDatasetName;

    public ProjectDatasetResultContent()
    {
        Padding = new Thickness(24);
        Background = GetThemeBrush("SystemControlBackgroundChromeMediumLowBrush");
        CornerRadius = new CornerRadius(12);

        _summaryLabel = new TextBlock
        {
            Text = string.Empty,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12)
        };

        _fileOutcomesPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 4
        };

        Child = new StackPanel
        {
            Spacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Parse result",
                    FontSize = 18,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 8)
                },
                _summaryLabel,
                _fileOutcomesPanel
            }
        };
    }

    public void SetDataset(string projectName, string datasetName)
    {
        _currentProjectName = projectName;
        _currentDatasetName = datasetName;
        Refresh();
    }

    public void Refresh()
    {
        if (_currentProjectName is null || _currentDatasetName is null)
        {
            return;
        }

        _summaryLabel.Text = "Loading parse result...";
        _fileOutcomesPanel.Children.Clear();
        MessageRouter.SendMessage(new ParseResultRequest(_currentProjectName, _currentDatasetName));
    }

    internal void OnParseResultResponseReceived(ParseResultResponse response)
    {
        if (!response.ParseHasRun)
        {
            _summaryLabel.Text = "No parse has been run yet for this dataset.";
            _fileOutcomesPanel.Children.Clear();
            return;
        }

        _summaryLabel.Text = response.OverallError is not null
            ? $"Error: {response.OverallError}"
            : $"Parsed {response.ParsedAtUtc:g} UTC: {response.SuccessCount} succeeded, {response.FailureCount} failed, out of {response.TotalFileCount} file(s).";

        _fileOutcomesPanel.Children.Clear();
        foreach (var outcome in response.FileOutcomes)
        {
            _fileOutcomesPanel.Children.Add(new TextBlock
            {
                Text = outcome.Success ? $"OK  {outcome.FileName}" : $"FAIL  {outcome.FileName}: {outcome.ErrorMessage}",
                TextWrapping = TextWrapping.Wrap
            });
        }
    }

    private static Brush? GetThemeBrush(string resourceKey)
    {
        return Application.Current.Resources.TryGetValue(resourceKey, out var resource)
            ? resource as Brush
            : null;
    }
}
