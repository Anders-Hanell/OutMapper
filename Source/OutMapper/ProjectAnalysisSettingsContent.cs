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
    private readonly TextBox _channelABinSizeInput;
    private readonly TextBox _channelBNameInput;
    private readonly TextBox _channelBBinSizeInput;
    private readonly TextBlock _statusLabel;
    private string? _currentProjectFolder;
    private string? _currentAnalysisName;

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

        _channelABinSizeInput = new TextBox
        {
            Header = "First channel bin size",
            PlaceholderText = "e.g. 5",
            MinWidth = 280
        };

        _channelBNameInput = new TextBox
        {
            Header = "Second channel name",
            PlaceholderText = "e.g. PRx",
            MinWidth = 280
        };

        _channelBBinSizeInput = new TextBox
        {
            Header = "Second channel bin size",
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
                _channelABinSizeInput,
                _channelBNameInput,
                _channelBBinSizeInput,
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

        if (!double.TryParse(_channelABinSizeInput.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var channelABinSize))
        {
            _statusLabel.Text = "Enter a valid number for the first channel's bin size.";
            return;
        }

        if (!double.TryParse(_channelBBinSizeInput.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var channelBBinSize))
        {
            _statusLabel.Text = "Enter a valid number for the second channel's bin size.";
            return;
        }

        switch (TwoVariableAnalysisSettings.Create(cohortName, channelAName, channelABinSize, channelBName, channelBBinSize))
        {
            case Failure<TwoVariableAnalysisSettings> failure:
                _statusLabel.Text = failure.Error;
                break;
            case Success<TwoVariableAnalysisSettings> success:
                _statusLabel.Text = "Generating graph...";
                MessageRouter.SendMessage(new GenerateAnalysisGraphRequest(_currentProjectFolder, _currentAnalysisName, success.Value));
                break;
        }
    }

    internal void OnGenerateAnalysisGraphResponseReceived(GenerateAnalysisGraphResponse response)
    {
        if (!response.Success)
        {
            _statusLabel.Text = $"Could not generate graph: {response.ErrorMessage}";
            return;
        }

        var outputPath = AnalysisGraphPdfService.GeneratePdf(response.ProjectFolder, response.AnalysisName, response);

        _statusLabel.Text = outputPath is null
            ? $"Generated with {response.MatchedPatientCount} of {response.TotalPatientCount} patient(s) matched, but the PDF could not be written."
            : $"Generated with {response.MatchedPatientCount} of {response.TotalPatientCount} patient(s) matched. Saved to {outputPath}";
    }

    private static Brush? GetThemeBrush(string resourceKey)
    {
        return Application.Current.Resources.TryGetValue(resourceKey, out var resource)
            ? resource as Brush
            : null;
    }
}
