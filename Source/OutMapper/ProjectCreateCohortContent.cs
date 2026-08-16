using System.Collections.Immutable;
using Messages;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace OutMapper;

public sealed class ProjectCreateCohortContent : Border
{
    internal static ProjectCreateCohortContent? Current { get; private set; }

    private readonly IFilePicker _filePicker;
    private readonly TextBox _cohortNameInput;
    private readonly TextBlock _selectedFileLabel;
    private readonly StackPanel _linkedDatasetsPanel;
    private readonly TextBlock _linkedDatasetsPlaceholder;
    private readonly TextBlock _resultLabel;
    private string? _currentProjectName;
    private string? _selectedCsvFilePath;

    public ProjectCreateCohortContent() : this(new WindowsCsvFilePicker())
    {
    }

    internal ProjectCreateCohortContent(IFilePicker filePicker)
    {
        _filePicker = filePicker;

        Padding = new Thickness(24);
        Background = GetThemeBrush("SystemControlBackgroundChromeMediumLowBrush");
        CornerRadius = new CornerRadius(12);

        _cohortNameInput = new TextBox
        {
            PlaceholderText = "Enter cohort name",
            MinWidth = 280,
            Margin = new Thickness(0, 0, 0, 12)
        };

        var selectFileButton = new Button
        {
            Content = "Select CSV file",
            HorizontalAlignment = HorizontalAlignment.Left,
            MinHeight = 44
        };

        selectFileButton.Click += (_, _) => SelectCsvFile();

        _selectedFileLabel = new TextBlock
        {
            Text = "No CSV file selected.",
            Margin = new Thickness(0, 0, 0, 12)
        };

        _linkedDatasetsPlaceholder = new TextBlock
        {
            Text = "No datasets in this project yet."
        };

        _linkedDatasetsPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 4,
            Children = { _linkedDatasetsPlaceholder }
        };

        var createButton = new Button
        {
            Content = "Create",
            HorizontalAlignment = HorizontalAlignment.Left,
            MinHeight = 44,
            MinWidth = 120
        };

        createButton.Click += (_, _) => CreateCohort();

        _resultLabel = new TextBlock
        {
            Text = string.Empty,
            Margin = new Thickness(0, 8, 0, 0),
            Foreground = GetThemeBrush("SystemControlForegroundAccentBrush")
        };

        Child = new StackPanel
        {
            Spacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "Create a new cohort",
                    FontSize = 18,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 8)
                },
                _cohortNameInput,
                selectFileButton,
                _selectedFileLabel,
                new TextBlock { Text = "Link to dataset(s)", FontWeight = FontWeights.SemiBold },
                _linkedDatasetsPanel,
                createButton,
                _resultLabel
            }
        };

        Current = this;
    }

    public void SetProject(string? projectName)
    {
        _currentProjectName = projectName;
        _cohortNameInput.Text = string.Empty;
        _resultLabel.Text = string.Empty;
        _selectedCsvFilePath = null;
        _selectedFileLabel.Text = "No CSV file selected.";
        _linkedDatasetsPanel.Children.Clear();
        _linkedDatasetsPanel.Children.Add(_linkedDatasetsPlaceholder);

        if (projectName is not null)
        {
            MessageRouter.SendMessage(new DatasetListRequest(projectName));
        }
    }

    internal void OnDatasetListResponseReceived(DatasetListResponse response)
    {
        // GatewayToTaskManager guarantees that incoming messages are dispatched on the UI thread.
        if (!string.Equals(response.ProjectName, _currentProjectName, StringComparison.Ordinal))
        {
            return;
        }

        _linkedDatasetsPanel.Children.Clear();

        if (response.DatasetNames.Length == 0)
        {
            _linkedDatasetsPanel.Children.Add(_linkedDatasetsPlaceholder);
            return;
        }

        foreach (var datasetName in response.DatasetNames)
        {
            _linkedDatasetsPanel.Children.Add(new CheckBox { Content = datasetName });
        }
    }

    private async void SelectCsvFile()
    {
        var path = await _filePicker.PickFileAsync();
        if (path is null)
        {
            return;
        }

        _selectedCsvFilePath = path;
        _selectedFileLabel.Text = path;
    }

    private void CreateCohort()
    {
        if (_currentProjectName is null)
        {
            _resultLabel.Text = "No project selected.";
            return;
        }

        var cohortName = _cohortNameInput.Text?.Trim();
        if (string.IsNullOrWhiteSpace(cohortName))
        {
            _resultLabel.Text = "Please enter a cohort name.";
            return;
        }

        if (_selectedCsvFilePath is null)
        {
            _resultLabel.Text = "Please select a CSV file.";
            return;
        }

        var linkedDatasetNames = _linkedDatasetsPanel.Children
            .OfType<CheckBox>()
            .Where(checkBox => checkBox.IsChecked == true)
            .Select(checkBox => (string)checkBox.Content)
            .ToImmutableArray();

        _resultLabel.Text = $"Creating '{cohortName}'...";
        MessageRouter.SendMessage(new CreateCohortRequest(cohortName, _currentProjectName, _selectedCsvFilePath, linkedDatasetNames));
    }

    internal void OnCreateCohortResponseReceived(CreateCohortResponse response)
    {
        // GatewayToTaskManager guarantees that incoming messages are dispatched on the UI thread.
        if (!string.Equals(response.ProjectName, _currentProjectName, StringComparison.Ordinal))
        {
            return;
        }

        _resultLabel.Text = response.Success
            ? $"Created '{response.CohortName}'"
            : $"Failed to create '{response.CohortName}'";
    }

    private static Brush? GetThemeBrush(string resourceKey)
    {
        return Application.Current.Resources.TryGetValue(resourceKey, out var resource)
            ? resource as Brush
            : null;
    }
}
