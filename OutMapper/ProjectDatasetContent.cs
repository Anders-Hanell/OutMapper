using Messages;
using Microsoft.UI.Text;

namespace OutMapper;

public sealed class ProjectDatasetContent : Border
{
    internal static ProjectDatasetContent? Current { get; private set; }

    private readonly TextBlock _nameLabel;
    private readonly ContentControl _innerContentArea;
    private readonly ProjectDatasetParseContent _parseContent;
    private readonly ProjectDatasetResultContent _resultContent;
    private string? _currentProjectName;
    private string? _currentDatasetName;

    public ProjectDatasetContent()
    {
        Padding = new Thickness(24);
        Background = GetThemeBrush("SystemControlBackgroundChromeMediumLowBrush");
        CornerRadius = new CornerRadius(12);

        _nameLabel = new TextBlock
        {
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 16)
        };

        _parseContent = new ProjectDatasetParseContent();
        _resultContent = new ProjectDatasetResultContent();

        _innerContentArea = new ContentControl
        {
            Content = _parseContent
        };

        var innerSidebar = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            Padding = new Thickness(8)
        };

        var parseButton = new Button { Content = "Parse" };
        var resultButton = new Button { Content = "Result" };

        parseButton.Click += (_, _) => _innerContentArea.Content = _parseContent;
        resultButton.Click += (_, _) =>
        {
            _resultContent.Refresh();
            _innerContentArea.Content = _resultContent;
        };

        innerSidebar.Children.Add(parseButton);
        innerSidebar.Children.Add(resultButton);

        var innerGrid = new Grid();
        innerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        innerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(_innerContentArea, 1);
        innerGrid.Children.Add(innerSidebar);
        innerGrid.Children.Add(_innerContentArea);

        Child = new StackPanel
        {
            Children = { _nameLabel, innerGrid }
        };

        Current = this;
    }

    public void SetDataset(string projectName, string datasetName)
    {
        _currentProjectName = projectName;
        _currentDatasetName = datasetName;
        _nameLabel.Text = datasetName;
        _parseContent.SetDataset(projectName, datasetName);
        _resultContent.SetDataset(projectName, datasetName);
        _innerContentArea.Content = _parseContent;
    }

    internal void OnParseResultResponseReceived(ParseResultResponse response)
    {
        // GatewayToTaskManager guarantees that incoming messages are dispatched on the UI thread.
        if (!string.Equals(response.ProjectName, _currentProjectName, StringComparison.Ordinal) ||
            !string.Equals(response.DatasetName, _currentDatasetName, StringComparison.Ordinal))
        {
            return;
        }

        _parseContent.OnParseResultResponseReceived(response);
        _resultContent.OnParseResultResponseReceived(response);
    }

    private static Brush? GetThemeBrush(string resourceKey)
    {
        return Application.Current.Resources.TryGetValue(resourceKey, out var resource)
            ? resource as Brush
            : null;
    }
}
