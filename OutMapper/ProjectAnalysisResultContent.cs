using Messages;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace OutMapper;

public sealed class ProjectAnalysisResultContent : Border
{
    private readonly TextBlock _summaryLabel;
    private string? _currentProjectName;
    private string? _currentAnalysisName;

    public ProjectAnalysisResultContent()
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

        Child = new StackPanel
        {
            Spacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Generation result",
                    FontSize = 18,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 8)
                },
                _summaryLabel
            }
        };
    }

    public void SetAnalysis(string projectName, string analysisName)
    {
        _currentProjectName = projectName;
        _currentAnalysisName = analysisName;
        Refresh();
    }

    public void Refresh()
    {
        if (_currentProjectName is null || _currentAnalysisName is null)
        {
            return;
        }

        _summaryLabel.Text = "Loading generation result...";
        MessageRouter.SendMessage(new AnalysisResultRequest(_currentProjectName, _currentAnalysisName));
    }

    internal void OnAnalysisResultResponseReceived(AnalysisResultResponse response)
    {
        if (!response.GenerationHasRun)
        {
            _summaryLabel.Text = "No graph has been generated yet for this analysis.";
            return;
        }

        _summaryLabel.Text = response.Success
            ? $"Generated {response.GeneratedAtUtc:g} UTC: {response.MatchedPatientCount} of {response.TotalPatientCount} patient(s) matched " +
              $"(cohort '{response.CohortName}', channels '{response.ChannelAName}' / '{response.ChannelBName}')."
            : $"Generated {response.GeneratedAtUtc:g} UTC with an error: {response.ErrorMessage}";
    }

    private static Brush? GetThemeBrush(string resourceKey)
    {
        return Application.Current.Resources.TryGetValue(resourceKey, out var resource)
            ? resource as Brush
            : null;
    }
}
