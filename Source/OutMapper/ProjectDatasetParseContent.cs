using DataStructures;
using Messages;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace OutMapper;

public sealed class ProjectDatasetParseContent : Border
{
    private readonly ComboBox _delimiterComboBox;
    private readonly RadioButton _periodDecimalSeparatorOption;
    private readonly RadioButton _commaDecimalSeparatorOption;
    private readonly TextBox _timeColumnHeaderInput;
    private readonly TextBox _timestampFormatInput;
    private readonly TextBlock _statusLabel;
    private string? _currentProjectFolder;
    private string? _currentDatasetName;

    public ProjectDatasetParseContent()
    {
        Padding = new Thickness(24);
        Background = GetThemeBrush("SystemControlBackgroundChromeMediumLowBrush");
        CornerRadius = new CornerRadius(12);

        _delimiterComboBox = new ComboBox
        {
            Header = "Delimiter",
            MinWidth = 200,
            ItemsSource = new[] { "Comma (,)", "Semicolon (;)", "Tab", "Pipe (|)" },
            SelectedIndex = 0
        };

        _periodDecimalSeparatorOption = new RadioButton
        {
            GroupName = "DecimalSeparator",
            Content = "Period (.)",
            IsChecked = true
        };

        _commaDecimalSeparatorOption = new RadioButton
        {
            GroupName = "DecimalSeparator",
            Content = "Comma (,)"
        };

        _timeColumnHeaderInput = new TextBox
        {
            Header = "Time column header",
            PlaceholderText = "e.g. Time",
            MinWidth = 280
        };

        _timestampFormatInput = new TextBox
        {
            Header = "Timestamp format",
            PlaceholderText = "e.g. yyyy-MM-dd HH:mm:ss",
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
                new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Children = { new TextBlock { Text = "Decimal separator" }, _periodDecimalSeparatorOption, _commaDecimalSeparatorOption }
                },
                _timeColumnHeaderInput,
                _timestampFormatInput,
                parseButton,
                _statusLabel
            }
        };
    }

    public void SetDataset(string projectName, string datasetName)
    {
        _currentProjectFolder = projectName;
        _currentDatasetName = datasetName;
        _statusLabel.Text = string.Empty;
    }

    private void StartParse()
    {
        if (_currentProjectFolder is null || _currentDatasetName is null)
        {
            _statusLabel.Text = "No dataset selected.";
            return;
        }

        var delimiterChar = GetSelectedDelimiterChar();
        var decimalSeparatorChar = _commaDecimalSeparatorOption.IsChecked == true ? ',' : '.';
        var timeColumnHeader = _timeColumnHeaderInput.Text?.Trim() ?? string.Empty;
        var timestampFormat = _timestampFormatInput.Text?.Trim() ?? string.Empty;

        switch (CsvParseParams.Create(delimiterChar, decimalSeparatorChar, timeColumnHeader, timestampFormat))
        {
            case Failure<CsvParseParams> failure:
                _statusLabel.Text = failure.Error;
                break;
            case Success<CsvParseParams> success:
                _statusLabel.Text = "Parsing...";
                MessageRouter.SendMessage(new ParseDatasetRequest(_currentProjectFolder, _currentDatasetName, success.Value));
                break;
        }
    }

    private char GetSelectedDelimiterChar()
    {
        return _delimiterComboBox.SelectedIndex switch
        {
            1 => ';',
            2 => '\t',
            3 => '|',
            _ => ','
        };
    }

    internal void OnParseResultResponseReceived(ParseResultResponse response)
    {
        _statusLabel.Text = response.OverallError is not null
            ? $"Parse finished with an error: {response.OverallError}"
            : $"Parsed {response.SuccessCount} of {response.TotalFileCount} file(s) successfully.";
    }

    private static Brush? GetThemeBrush(string resourceKey)
    {
        return Application.Current.Resources.TryGetValue(resourceKey, out var resource)
            ? resource as Brush
            : null;
    }
}
