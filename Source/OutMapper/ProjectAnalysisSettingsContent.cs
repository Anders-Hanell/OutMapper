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
    private readonly ComboBox _channelAComboBox;
    private readonly TextBox _channelARangeStartInput;
    private readonly TextBox _channelARangeEndInput;
    private readonly TextBox _channelABinWidthInput;
    private readonly RadioButton _channelAIsLeftInclusiveOption;
    private readonly RadioButton _channelAIsRightInclusiveOption;
    private readonly ComboBox _channelBComboBox;
    private readonly TextBox _channelBRangeStartInput;
    private readonly TextBox _channelBRangeEndInput;
    private readonly TextBox _channelBBinWidthInput;
    private readonly RadioButton _channelBIsLeftInclusiveOption;
    private readonly RadioButton _channelBIsRightInclusiveOption;
    private readonly TextBlock _noChannelsLabel;
    private readonly TextBlock _statusLabel;
    private string? _currentProjectFolder;
    private string? _currentAnalysisName;
    private string? _pendingRangeWarning;
    private TwoVariableAnalysisSettings? _pendingSettingsToApply;

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

        _cohortComboBox.SelectionChanged += (_, _) => OnCohortSelectionChanged();

        _noCohortsLabel = new TextBlock
        {
            Text = "No cohorts in this project yet."
        };

        _channelAComboBox = new ComboBox
        {
            Header = "First channel name",
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

        _channelAIsLeftInclusiveOption = new RadioButton
        {
            GroupName = "ChannelAInclusivity",
            Content = "Left-inclusive",
            IsChecked = true
        };

        _channelAIsRightInclusiveOption = new RadioButton
        {
            GroupName = "ChannelAInclusivity",
            Content = "Right-inclusive"
        };

        _channelBComboBox = new ComboBox
        {
            Header = "Second channel name",
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

        _channelBIsLeftInclusiveOption = new RadioButton
        {
            GroupName = "ChannelBInclusivity",
            Content = "Left-inclusive",
            IsChecked = true
        };

        _channelBIsRightInclusiveOption = new RadioButton
        {
            GroupName = "ChannelBInclusivity",
            Content = "Right-inclusive"
        };

        _noChannelsLabel = new TextBlock
        {
            Text = "No channels found for this cohort's linked dataset(s).",
            Visibility = Visibility.Collapsed
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

        Child = new ScrollViewer
        {
            VerticalScrollMode = ScrollMode.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollMode = ScrollMode.Disabled,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = new StackPanel
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
                    _channelAComboBox,
                    _channelARangeStartInput,
                    _channelARangeEndInput,
                    _channelABinWidthInput,
                    new StackPanel
                    {
                        Orientation = Orientation.Vertical,
                        Children =
                        {
                            new TextBlock { Text = "First channel bin inclusivity" },
                            _channelAIsLeftInclusiveOption,
                            _channelAIsRightInclusiveOption
                        }
                    },
                    _channelBComboBox,
                    _channelBRangeStartInput,
                    _channelBRangeEndInput,
                    _channelBBinWidthInput,
                    new StackPanel
                    {
                        Orientation = Orientation.Vertical,
                        Children =
                        {
                            new TextBlock { Text = "Second channel bin inclusivity" },
                            _channelBIsLeftInclusiveOption,
                            _channelBIsRightInclusiveOption
                        }
                    },
                    _noChannelsLabel,
                    generateButton,
                    _statusLabel
                }
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
        _pendingSettingsToApply = null;

        _cohortComboBox.Items.Clear();
        _noCohortsLabel.Visibility = Visibility.Visible;

        _channelAComboBox.Items.Clear();
        _channelBComboBox.Items.Clear();
        _noChannelsLabel.Visibility = Visibility.Collapsed;

        _channelARangeStartInput.Text = string.Empty;
        _channelARangeEndInput.Text = string.Empty;
        _channelABinWidthInput.Text = string.Empty;
        _channelAIsLeftInclusiveOption.IsChecked = true;

        _channelBRangeStartInput.Text = string.Empty;
        _channelBRangeEndInput.Text = string.Empty;
        _channelBBinWidthInput.Text = string.Empty;
        _channelBIsLeftInclusiveOption.IsChecked = true;

        MessageRouter.SendMessage(new CohortListRequest(projectName));
        MessageRouter.SendMessage(new AnalysisSettingsRequest(projectName, analysisName));
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

        if (_pendingSettingsToApply is { } pendingSettings && response.CohortNames.Contains(pendingSettings.CohortName))
        {
            _cohortComboBox.SelectedItem = pendingSettings.CohortName;
        }
    }

    private void OnCohortSelectionChanged()
    {
        _channelAComboBox.Items.Clear();
        _channelBComboBox.Items.Clear();
        _noChannelsLabel.Visibility = Visibility.Collapsed;

        if (_currentProjectFolder is null || _cohortComboBox.SelectedItem is not string cohortName ||
            string.IsNullOrWhiteSpace(cohortName))
        {
            return;
        }

        MessageRouter.SendMessage(new ChannelListRequest(_currentProjectFolder, cohortName));
    }

    internal void OnChannelListResponseReceived(ChannelListResponse response)
    {
        // GatewayToTaskManager guarantees that incoming messages are dispatched on the UI thread.
        if (!string.Equals(response.ProjectFolder, _currentProjectFolder, StringComparison.Ordinal) ||
            !string.Equals(response.CohortName, _cohortComboBox.SelectedItem as string, StringComparison.Ordinal))
        {
            return;
        }

        _channelAComboBox.Items.Clear();
        _channelBComboBox.Items.Clear();

        foreach (var channelName in response.ChannelNames)
        {
            _channelAComboBox.Items.Add(channelName);
            _channelBComboBox.Items.Add(channelName);
        }

        _noChannelsLabel.Visibility = response.ChannelNames.Length == 0 ? Visibility.Visible : Visibility.Collapsed;

        if (_pendingSettingsToApply is { } pendingSettings && string.Equals(pendingSettings.CohortName, response.CohortName, StringComparison.Ordinal))
        {
            if (response.ChannelNames.Contains(pendingSettings.ChannelAGrid.ChannelName))
            {
                _channelAComboBox.SelectedItem = pendingSettings.ChannelAGrid.ChannelName;
            }

            if (response.ChannelNames.Contains(pendingSettings.ChannelBGrid.ChannelName))
            {
                _channelBComboBox.SelectedItem = pendingSettings.ChannelBGrid.ChannelName;
            }

            _pendingSettingsToApply = null;
        }
    }

    internal void OnAnalysisSettingsResponseReceived(AnalysisSettingsResponse response)
    {
        // GatewayToTaskManager guarantees that incoming messages are dispatched on the UI thread.
        if (!string.Equals(response.ProjectFolder, _currentProjectFolder, StringComparison.Ordinal) ||
            !string.Equals(response.AnalysisName, _currentAnalysisName, StringComparison.Ordinal))
        {
            return;
        }

        if (!response.Found || response.Settings is null)
        {
            return;
        }

        var settings = response.Settings;
        _pendingSettingsToApply = settings;

        _channelARangeStartInput.Text = settings.ChannelAGrid.LowerLimit.ToString(CultureInfo.InvariantCulture);
        _channelARangeEndInput.Text = settings.ChannelAGrid.UpperLimit.ToString(CultureInfo.InvariantCulture);
        _channelABinWidthInput.Text = settings.ChannelAGrid.BinSize.ToString(CultureInfo.InvariantCulture);
        _channelAIsLeftInclusiveOption.IsChecked = settings.ChannelAGrid.IsLeftInclusive;
        _channelAIsRightInclusiveOption.IsChecked = !settings.ChannelAGrid.IsLeftInclusive;

        _channelBRangeStartInput.Text = settings.ChannelBGrid.LowerLimit.ToString(CultureInfo.InvariantCulture);
        _channelBRangeEndInput.Text = settings.ChannelBGrid.UpperLimit.ToString(CultureInfo.InvariantCulture);
        _channelBBinWidthInput.Text = settings.ChannelBGrid.BinSize.ToString(CultureInfo.InvariantCulture);
        _channelBIsLeftInclusiveOption.IsChecked = settings.ChannelBGrid.IsLeftInclusive;
        _channelBIsRightInclusiveOption.IsChecked = !settings.ChannelBGrid.IsLeftInclusive;

        if (_cohortComboBox.Items.Contains(settings.CohortName))
        {
            _cohortComboBox.SelectedItem = settings.CohortName;
        }
    }

    private void GenerateGraph()
    {
        if (_currentProjectFolder is null || _currentAnalysisName is null)
        {
            _statusLabel.Text = "No analysis selected.";
            return;
        }

        var cohortName = _cohortComboBox.SelectedItem as string ?? string.Empty;
        var channelAName = _channelAComboBox.SelectedItem as string ?? string.Empty;
        var channelBName = _channelBComboBox.SelectedItem as string ?? string.Empty;

        if (string.IsNullOrWhiteSpace(channelAName))
        {
            _statusLabel.Text = "Select a name for the first channel.";
            return;
        }

        if (string.IsNullOrWhiteSpace(channelBName))
        {
            _statusLabel.Text = "Select a name for the second channel.";
            return;
        }

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

        var channelAIsLeftInclusive = _channelAIsLeftInclusiveOption.IsChecked == true;
        var channelBIsLeftInclusive = _channelBIsLeftInclusiveOption.IsChecked == true;

        NumericGridDef channelAGrid;
        switch (NumericGridDef.Create(channelAName, channelARangeStart, channelARangeEnd, channelABinWidth, channelAIsLeftInclusive))
        {
            case Failure<NumericGridDef> failure:
                _statusLabel.Text = failure.Error;
                return;
            case Success<NumericGridDef> success:
                channelAGrid = success.Value;
                break;
            default:
                return;
        }

        NumericGridDef channelBGrid;
        switch (NumericGridDef.Create(channelBName, channelBRangeStart, channelBRangeEnd, channelBBinWidth, channelBIsLeftInclusive))
        {
            case Failure<NumericGridDef> failure:
                _statusLabel.Text = failure.Error;
                return;
            case Success<NumericGridDef> success:
                channelBGrid = success.Value;
                break;
            default:
                return;
        }

        switch (TwoVariableAnalysisSettings.Create(cohortName, channelAGrid, channelBGrid))
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
