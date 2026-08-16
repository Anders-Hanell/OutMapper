using Messages;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace OutMapper;

public sealed class ProjectCohortResultContent : Border
{
    private readonly TextBlock _summaryLabel;
    private string? _currentProjectFolder;
    private string? _currentCohortName;

    public ProjectCohortResultContent()
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
                    Text = "Parse result",
                    FontSize = 18,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 8)
                },
                _summaryLabel
            }
        };
    }

    public void SetCohort(string projectName, string cohortName)
    {
        _currentProjectFolder = projectName;
        _currentCohortName = cohortName;
        Refresh();
    }

    public void Refresh()
    {
        if (_currentProjectFolder is null || _currentCohortName is null)
        {
            return;
        }

        _summaryLabel.Text = "Loading parse result...";
        MessageRouter.SendMessage(new CohortParseResultRequest(_currentProjectFolder, _currentCohortName));
    }

    internal void OnCohortParseResultResponseReceived(CohortParseResultResponse response)
    {
        if (!response.ParseHasRun)
        {
            _summaryLabel.Text = "No parse has been run yet for this cohort.";
            return;
        }

        _summaryLabel.Text = response.Success
            ? $"Parsed {response.ParsedAtUtc:g} UTC: {response.PatientCount} patient(s)."
            : $"Parsed {response.ParsedAtUtc:g} UTC with an error: {response.ErrorMessage}";
    }

    private static Brush? GetThemeBrush(string resourceKey)
    {
        return Application.Current.Resources.TryGetValue(resourceKey, out var resource)
            ? resource as Brush
            : null;
    }
}
