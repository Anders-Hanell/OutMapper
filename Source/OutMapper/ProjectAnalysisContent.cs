using Messages;
using Microsoft.UI.Text;

namespace OutMapper;

public sealed class ProjectAnalysisContent : Border
{
    internal static ProjectAnalysisContent? Current { get; private set; }

    private readonly TextBlock _nameLabel;
    private readonly ContentControl _innerContentArea;
    private readonly ProjectAnalysisSettingsContent _settingsContent;
    private readonly ProjectAnalysisResultContent _resultContent;
    private string? _currentProjectFolder;
    private string? _currentAnalysisName;

    public ProjectAnalysisContent()
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

        _settingsContent = new ProjectAnalysisSettingsContent();
        _resultContent = new ProjectAnalysisResultContent();

        _innerContentArea = new ContentControl
        {
            Content = _settingsContent
        };

        var innerSidebar = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            Padding = new Thickness(8)
        };

        var settingsButton = new Button { Content = "Settings" };
        var resultButton = new Button { Content = "Result" };

        settingsButton.Click += (_, _) => _innerContentArea.Content = _settingsContent;
        resultButton.Click += (_, _) =>
        {
            _resultContent.Refresh();
            _innerContentArea.Content = _resultContent;
        };

        innerSidebar.Children.Add(settingsButton);
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

    public void SetAnalysis(string projectName, string analysisName)
    {
        _currentProjectFolder = projectName;
        _currentAnalysisName = analysisName;
        _nameLabel.Text = analysisName;
        _settingsContent.SetAnalysis(projectName, analysisName);
        _resultContent.SetAnalysis(projectName, analysisName);
        _innerContentArea.Content = _settingsContent;
    }

    internal void OnGenerateAnalysisGraphResponseReceived(GenerateAnalysisGraphResponse response)
    {
        // GatewayToTaskManager guarantees that incoming messages are dispatched on the UI thread.
        if (!string.Equals(response.ProjectFolder, _currentProjectFolder, StringComparison.Ordinal) ||
            !string.Equals(response.AnalysisName, _currentAnalysisName, StringComparison.Ordinal))
        {
            return;
        }

        _settingsContent.OnGenerateAnalysisGraphResponseReceived(response);
    }

    internal void OnAnalysisResultResponseReceived(AnalysisResultResponse response)
    {
        // GatewayToTaskManager guarantees that incoming messages are dispatched on the UI thread.
        if (!string.Equals(response.ProjectFolder, _currentProjectFolder, StringComparison.Ordinal) ||
            !string.Equals(response.AnalysisName, _currentAnalysisName, StringComparison.Ordinal))
        {
            return;
        }

        _resultContent.OnAnalysisResultResponseReceived(response);
    }

    private static Brush? GetThemeBrush(string resourceKey)
    {
        return Application.Current.Resources.TryGetValue(resourceKey, out var resource)
            ? resource as Brush
            : null;
    }
}
