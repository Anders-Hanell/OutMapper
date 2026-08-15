using System.IO.Compression;
using Messages;
using Microsoft.UI.Text;
using Uno.Toolkit.UI;

namespace OutMapper;

internal sealed class ProjectsPanel : Grid
{
    internal static ProjectsPanel? Current { get; private set; }

    private readonly TextBlock _projectNameLabel;
    private readonly ContentControl _contentArea;
    private readonly StackPanel _navigationPanel;
    private readonly TextBlock _placeholderLabel;
    private readonly Border _placeholderContent;
    private readonly ProjectDatasetContent _datasetContent;
    private readonly ProjectCreateDatasetContent _createDatasetContent;
    private readonly ProjectCohortContent _cohortContent;
    private readonly ProjectCreateCohortContent _createCohortContent;
    private readonly Button _createDatasetButton;
    private readonly Button _createCohortButton;
    private readonly Button _generatePdfButton;
    private string? _currentProjectName;
    private ImmutableArray<string> _datasetNames = ImmutableArray<string>.Empty;
    private ImmutableArray<string> _cohortNames = ImmutableArray<string>.Empty;

    public ProjectsPanel()
    {
        Padding = new Thickness(16);

        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _placeholderLabel = new TextBlock
        {
            Text = "No project selected.",
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };

        _placeholderContent = new Border
        {
            Padding = new Thickness(24),
            Background = GetThemeBrush("SystemControlBackgroundChromeMediumLowBrush"),
            CornerRadius = new CornerRadius(12),
            Child = _placeholderLabel
        };

        _datasetContent = new ProjectDatasetContent();
        _createDatasetContent = new ProjectCreateDatasetContent();
        _cohortContent = new ProjectCohortContent();
        _createCohortContent = new ProjectCreateCohortContent();

        _contentArea = new ContentControl
        {
            Content = _placeholderContent
        };

        _createDatasetButton = new Button { Content = "Create dataset" };
        _createCohortButton = new Button { Content = "Create cohort" };
        _generatePdfButton = new Button { Content = "Generate pdf" };

        _createDatasetButton.Click += (_, _) => _contentArea.Content = _createDatasetContent;
        _createCohortButton.Click += (_, _) => _contentArea.Content = _createCohortContent;
        _generatePdfButton.Click += (_, _) => GraphPdfService.GeneratePdf();

        _navigationPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            Padding = new Thickness(8)
        };

        _projectNameLabel = new TextBlock
        {
            Text = "No project selected.",
            FontSize = 20,
            Margin = new Thickness(16, 8, 0, 16)
        };

        Grid.SetRowSpan(_navigationPanel, 2);
        Grid.SetColumn(_projectNameLabel, 1);
        Grid.SetRow(_contentArea, 1);
        Grid.SetColumn(_contentArea, 1);

        Children.Add(_navigationPanel);
        Children.Add(_projectNameLabel);
        Children.Add(_contentArea);

        RebuildNavigationButtons();

        Current = this;
    }

    public void Refresh()
    {
        var selectedProject = ProjectFolderService.GetSelectedProjectName(out var error);
        _currentProjectName = selectedProject;

        _projectNameLabel.Text = error ?? (selectedProject is null
            ? "No project selected."
            : $"Current project: {selectedProject}");

        _createDatasetContent.SetProject(selectedProject);
        _createCohortContent.SetProject(selectedProject);
        _contentArea.Content = _placeholderContent;
        _datasetNames = ImmutableArray<string>.Empty;
        _cohortNames = ImmutableArray<string>.Empty;

        if (selectedProject is null)
        {
            _placeholderLabel.Text = "No project selected.";
            RebuildNavigationButtons();
            return;
        }

        _placeholderLabel.Text = "Loading datasets and cohorts...";
        RebuildNavigationButtons();
        MessageRouter.SendMessage(new DatasetListRequest(selectedProject));
        MessageRouter.SendMessage(new CohortListRequest(selectedProject));
    }

    internal void OnDatasetListResponseReceived(DatasetListResponse response)
    {
        // GatewayToTaskManager guarantees that incoming messages are dispatched on the UI thread.
        if (!string.Equals(response.ProjectName, _currentProjectName, StringComparison.Ordinal))
        {
            return;
        }

        _datasetNames = response.DatasetNames;
        _placeholderLabel.Text = response.DatasetNames.Length > 0
            ? "Select a dataset or cohort, or create a new one."
            : "No datasets yet. Create one to get started.";

        RebuildNavigationButtons();
    }

    internal void OnCreateDatasetResponseReceived(CreateDatasetResponse response)
    {
        // GatewayToTaskManager guarantees that incoming messages are dispatched on the UI thread.
        if (!string.Equals(response.ProjectName, _currentProjectName, StringComparison.Ordinal))
        {
            return;
        }

        _createDatasetContent.OnCreateDatasetResponseReceived(response);

        if (response.Success)
        {
            MessageRouter.SendMessage(new DatasetListRequest(response.ProjectName!));
        }
    }

    internal void OnCohortListResponseReceived(CohortListResponse response)
    {
        // GatewayToTaskManager guarantees that incoming messages are dispatched on the UI thread.
        if (!string.Equals(response.ProjectName, _currentProjectName, StringComparison.Ordinal))
        {
            return;
        }

        _cohortNames = response.CohortNames;
        RebuildNavigationButtons();
    }

    internal void OnCreateCohortResponseReceived(CreateCohortResponse response)
    {
        // GatewayToTaskManager guarantees that incoming messages are dispatched on the UI thread.
        if (!string.Equals(response.ProjectName, _currentProjectName, StringComparison.Ordinal))
        {
            return;
        }

        _createCohortContent.OnCreateCohortResponseReceived(response);

        if (response.Success)
        {
            MessageRouter.SendMessage(new CohortListRequest(response.ProjectName!));
        }
    }

    private void RebuildNavigationButtons()
    {
        _navigationPanel.Children.Clear();

        _navigationPanel.Children.Add(new TextBlock { Text = "Datasets", FontWeight = FontWeights.SemiBold });
        _navigationPanel.Children.Add(_createDatasetButton);

        foreach (var datasetName in _datasetNames)
        {
            var datasetButton = new Button { Content = datasetName };
            datasetButton.Click += (_, _) =>
            {
                _datasetContent.SetDataset(_currentProjectName!, datasetName);
                _contentArea.Content = _datasetContent;
            };
            _navigationPanel.Children.Add(datasetButton);
        }

        _navigationPanel.Children.Add(new TextBlock { Text = "Cohorts", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 12, 0, 0) });
        _navigationPanel.Children.Add(_createCohortButton);

        foreach (var cohortName in _cohortNames)
        {
            var cohortButton = new Button { Content = cohortName };
            cohortButton.Click += (_, _) =>
            {
                _cohortContent.SetCohort(_currentProjectName!, cohortName);
                _contentArea.Content = _cohortContent;
            };
            _navigationPanel.Children.Add(cohortButton);
        }

        _navigationPanel.Children.Add(new Border()
        {
            Height = 10,
            Width = 150,
            CornerRadius = 5,
            Background = new SolidColorBrush(Microsoft.UI.Colors.DarkRed)
        });

        _navigationPanel.Children.Add(_generatePdfButton);
    }

    private static Brush? GetThemeBrush(string resourceKey)
    {
        return Application.Current.Resources.TryGetValue(resourceKey, out var resource)
            ? resource as Brush
            : null;
    }
}
