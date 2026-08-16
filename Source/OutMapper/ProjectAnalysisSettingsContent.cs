using System.Globalization;
using DataStructures;
using Messages;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace OutMapper;

public sealed class ProjectAnalysisSettingsContent : Border
{
    internal static ProjectAnalysisSettingsContent? Current { get; private set; }

    private readonly ComboBox _cohortComboBox;
    private readonly TextBlock _noCohortsLabel;
    private readonly TextBox _channelANameInput;
    private readonly TextBox _channelARangeStartInput;
    private readonly TextBox _channelARangeEndInput;
    private readonly TextBox _channelABinWidthInput;
    private readonly TextBox _channelBNameInput;
    private readonly TextBox _channelBRangeStartInput;
    private readonly TextBox _channelBRangeEndInput;
    private readonly TextBox _channelBBinWidthInput;
    private readonly TextBlock _statusLabel;
    private string? _currentProjectFolder;
    private string? _currentAnalysisName;
    private string? _pendingRangeWarning;

    public ProjectAnalysisSettingsContent()
    {
        Padding = new Thickness(24);
        Background = GetThemeBrush("SystemControlBackgroundChromeMediumLowBrush");
        CornerRadius = new CornerRadius(12);

        _cohortComboBox = new ComboBox
        {
            Header = "Cohort",
            MinWidth = 280
        };

        _noCohortsLabel = new TextBlock
        {
            Text = "No cohorts in this project yet."
        };

        _channelANameInput = new TextBox
        {
            Header = "First channel name",
            PlaceholderText = "e.g. ICP",
            MinWidth = 280
        };

        _channelARangeStartInput = new TextBox
        {
            Header = "First channel range start",
            PlaceholderText = "e.g. 0",
            MinWidth = 280
        };

        _channelARangeEndInput = new TextBox
        {
            Header = "First channel range end",
            PlaceholderText = "e.g. 100",
            MinWidth = 280
        };

        _channelABinWidthInput = new TextBox
        {
            Header = "First channel bin width",
            PlaceholderText = "e.g. 5",
            MinWidth = 280
        };

        _channelBNameInput = new TextBox
        {
            Header = "Second channel name",
            PlaceholderText = "e.g. PRx",
            MinWidth = 280
        };

        _channelBRangeStartInput = new TextBox
        {
            Header = "Second channel range start",
            PlaceholderText = "e.g. -1",
            MinWidth = 280
        };

        _channelBRangeEndInput = new TextBox
        {
            Header = "Second channel range end",
            PlaceholderText = "e.g. 1",
            MinWidth = 280
        };

        _channelBBinWidthInput = new TextBox
        {
            Header = "Second channel bin width",
            PlaceholderText = "e.g. 0.1",
            MinWidth = 280
        };

        var generateButton = new Button
        {
            Content = "Generate graph",
            HorizontalAlignment = HorizontalAlignment.Left,
            MinHeight = 44,
            MinWidth = 160
        };

        generateButton.Click += (_, _) => GenerateGraph();

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
                    Text = "Analysis settings",
                    FontSize = 18,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 8)
                },
                _cohortComboBox,
                _noCohortsLabel,
                _channelANameInput,
                _channelARangeStartInput,
                _channelARangeEndInput,
                _channelABinWidthInput,
                _channelBNameInput,
                _channelBRangeStartInput,
                _channelBRangeEndInput,
                _channelBBinWidthInput,
                generateButton,
                _statusLabel
            }
        };

        Current = this;
    }

    public void SetAnalysis(string projectName, string analysisName)
    {
        _currentProjectFolder = projectName;
        _currentAnalysisName = analysisName;
        _statusLabel.Text = string.Empty;
        _pendingRangeWarning = null;
        _cohortComboBox.Items.Clear();
        _noCohortsLabel.Visibility = Visibility.Visible;

        MessageRouter.SendMessage(new CohortListRequest(projectName));
    }

    internal void OnCohortListResponseReceived(CohortListResponse response)
    {
        // GatewayToTaskManager guarantees that incoming messages are dispatched on the UI thread.
        if (!string.Equals(response.ProjectFolder, _currentProjectFolder, StringComparison.Ordinal))
        {
            return;
        }

        _cohortComboBox.Items.Clear();

        foreach (var cohortName in response.CohortNames)
        {
            _cohortComboBox.Items.Add(cohortName);
        }

        _noCohortsLabel.Visibility = response.CohortNames.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void GenerateGraph()
    {
        if (_currentProjectFolder is null || _currentAnalysisName is null)
        {
            _statusLabel.Text = "No analysis selected.";
            return;
        }

        var cohortName = _cohortComboBox.SelectedItem as string ?? string.Empty;
        var channelAName = _channelANameInput.Text?.Trim() ?? string.Empty;
        var channelBName = _channelBNameInput.Text?.Trim() ?? string.Empty;

        if (!TryParse(_channelARangeStartInput.Text, out var channelARangeStart))
        {
            _statusLabel.Text = "Enter a valid number for the first channel's range start.";
            return;
        }

        if (!TryParse(_channelARangeEndInput.Text, out var channelARangeEnd))
        {
            _statusLabel.Text = "Enter a valid number for the first channel's range end.";
            return;
        }

        if (!TryParse(_channelABinWidthInput.Text, out var channelABinWidth))
        {
            _statusLabel.Text = "Enter a valid number for the first channel's bin width.";
            return;
        }

        if (!TryParse(_channelBRangeStartInput.Text, out var channelBRangeStart))
        {
            _statusLabel.Text = "Enter a valid number for the second channel's range start.";
            return;
        }

        if (!TryParse(_channelBRangeEndInput.Text, out var channelBRangeEnd))
        {
            _statusLabel.Text = "Enter a valid number for the second channel's range end.";
            return;
        }

        if (!TryParse(_channelBBinWidthInput.Text, out var channelBBinWidth))
        {
            _statusLabel.Text = "Enter a valid number for the second channel's bin width.";
            return;
        }

        switch (TwoVariableAnalysisSettings.Create(
            cohortName,
            channelAName, channelARangeStart, channelARangeEnd, channelABinWidth,
            channelBName, channelBRangeStart, channelBRangeEnd, channelBBinWidth))
        {
            case Failure<TwoVariableAnalysisSettings> failure:
                _statusLabel.Text = failure.Error;
                break;
            case Success<TwoVariableAnalysisSettings> success:
                _pendingRangeWarning = success.Value.RangeWarning;
                _statusLabel.Text = _pendingRangeWarning is null
                    ? "Generating graph..."
                    : $"{_pendingRangeWarning} Generating graph...";
                MessageRouter.SendMessage(new GenerateAnalysisGraphRequest(_currentProjectFolder, _currentAnalysisName, success.Value));
                break;
        }
    }

    private static bool TryParse(string? text, out double value)
    {
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    internal void OnGenerateAnalysisGraphResponseReceived(GenerateAnalysisGraphResponse response)
    {
        if (!response.Success)
        {
            _statusLabel.Text = $"Could not generate graph: {response.ErrorMessage}";
            return;
        }

        var outputPath = AnalysisGraphPdfService.GeneratePdf(response.ProjectFolder, response.AnalysisName, response);

        var resultMessage = outputPath is null
            ? $"Generated with {response.MatchedPatientCount} of {response.TotalPatientCount} patient(s) matched, but the PDF could not be written."
            : $"Generated with {response.MatchedPatientCount} of {response.TotalPatientCount} patient(s) matched. Saved to {outputPath}";

        _statusLabel.Text = _pendingRangeWarning is null ? resultMessage : $"{_pendingRangeWarning} {resultMessage}";
        _pendingRangeWarning = null;
    }

    private static Brush? GetThemeBrush(string resourceKey)
    {
        return Application.Current.Resources.TryGetValue(resourceKey, out var resource)
            ? resource as Brush
            : null;
    }
}
