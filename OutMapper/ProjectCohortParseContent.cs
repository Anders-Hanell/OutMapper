using DataStructures;
using Messages;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace OutMapper;

public sealed class ProjectCohortParseContent : Border
{
    private readonly ComboBox _delimiterComboBox;
    private readonly TextBox _patientIdColumnHeaderInput;
    private readonly TextBox _outcomeColumnHeaderInput;
    private readonly TextBlock _statusLabel;
    private string? _currentProjectName;
    private string? _currentCohortName;

    public ProjectCohortParseContent()
    {
        Padding = new Thickness(24);
        Background = GetThemeBrush("SystemControlBackgroundChromeMediumLowBrush");
        CornerRadius = new CornerRadius(12);

        _delimiterComboBox = new ComboBox
        {
            Header = "Delimiter",
            MinWidth = 200,
            ItemsSource = new[] { "Comma (,)", "Semicolon (;)" },
            SelectedIndex = 0
        };

        _patientIdColumnHeaderInput = new TextBox
        {
            Header = "Patient ID column header",
            PlaceholderText = "e.g. PatientId",
            MinWidth = 280
        };

        _outcomeColumnHeaderInput = new TextBox
        {
            Header = "Outcome column header",
            PlaceholderText = "e.g. Outcome",
            MinWidth = 280
        };

        var parseButton = new Button
        {
            Content = "Parse",
            HorizontalAlignment = HorizontalAlignment.Left,
            MinHeight = 44,
            MinWidth = 120
        };

        parseButton.Click += (_, _) => StartParse();

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
                    Text = "Parse parameters",
                    FontSize = 18,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 8)
                },
                _delimiterComboBox,
                _patientIdColumnHeaderInput,
                _outcomeColumnHeaderInput,
                parseButton,
                _statusLabel
            }
        };
    }

    public void SetCohort(string projectName, string cohortName)
    {
        _currentProjectName = projectName;
        _currentCohortName = cohortName;
        _statusLabel.Text = string.Empty;
    }

    private void StartParse()
    {
        if (_currentProjectName is null || _currentCohortName is null)
        {
            _statusLabel.Text = "No cohort selected.";
            return;
        }

        var delimiterChar = _delimiterComboBox.SelectedIndex == 1 ? ';' : ',';
        var patientIdColumnHeader = _patientIdColumnHeaderInput.Text?.Trim() ?? string.Empty;
        var outcomeColumnHeader = _outcomeColumnHeaderInput.Text?.Trim() ?? string.Empty;

        switch (CohortParseParams.Create(delimiterChar, patientIdColumnHeader, outcomeColumnHeader))
        {
            case Failure<CohortParseParams> failure:
                _statusLabel.Text = failure.Error;
                break;
            case Success<CohortParseParams> success:
                _statusLabel.Text = "Parsing...";
                MessageRouter.SendMessage(new ParseCohortRequest(_currentProjectName, _currentCohortName, success.Value));
                break;
        }
    }

    internal void OnCohortParseResultResponseReceived(CohortParseResultResponse response)
    {
        if (!response.ParseHasRun)
        {
            // This response only reflects that no parse has been attempted yet (e.g. from the Result
            // tab's own request); it isn't the outcome of a parse triggered from this panel.
            return;
        }

        _statusLabel.Text = response.Success
            ? $"Parsed successfully: {response.PatientCount} patient(s)."
            : $"Parse finished with an error: {response.ErrorMessage}";
    }

    private static Brush? GetThemeBrush(string resourceKey)
    {
        return Application.Current.Resources.TryGetValue(resourceKey, out var resource)
            ? resource as Brush
            : null;
    }
}
