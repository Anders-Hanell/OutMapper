using System.IO.Compression;
using Messages;
using Microsoft.UI.Text;
using Uno.Toolkit.UI;

namespace OutMapper;

internal sealed class ProjectsPanel : Grid, IRefreshable
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
    private readonly ProjectAnalysisContent _analysisContent;
    private readonly ProjectCreateAnalysisContent _createAnalysisContent;
    private readonly ProjectFigureContent _figureContent;
    private readonly ProjectCreateFigureContent _createFigureContent;
    private readonly Button _createDatasetButton;
    private readonly Button _createCohortButton;
    private readonly Button _createAnalysisButton;
    private readonly Button _createFigureButton;
    private string? _currentProjectFolder;
    private ImmutableArray<string> _datasetNames = ImmutableArray<string>.Empty;
    private ImmutableArray<string> _cohortNames = ImmutableArray<string>.Empty;
    private ImmutableArray<string> _analysisNames = ImmutableArray<string>.Empty;
    private ImmutableArray<string> _figureNames = ImmutableArray<string>.Empty;

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
        _analysisContent = new ProjectAnalysisContent();
        _createAnalysisContent = new ProjectCreateAnalysisContent();
        _figureContent = new ProjectFigureContent();
        _createFigureContent = new ProjectCreateFigureContent();

        _contentArea = new ContentControl
        {
            Content = _placeholderContent
        };

        _createDatasetButton = new Button { Content = "Create dataset" };
        _createCohortButton = new Button { Content = "Create cohort" };
        _createAnalysisButton = new Button { Content = "Create analysis" };
        _createFigureButton = new Button { Content = "Create figure" };

        _createDatasetButton.Click += (_, _) => _contentArea.Content = _createDatasetContent;
        _createCohortButton.Click += (_, _) => _contentArea.Content = _createCohortContent;
        _createAnalysisButton.Click += (_, _) => _contentArea.Content = _createAnalysisContent;
        _createFigureButton.Click += (_, _) => _contentArea.Content = _createFigureContent;

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
        var selectedProjectFolder = ProjectFolderService.GetCurrentProjectFolder();
        var selectedProjectName = ProjectFolderService.GetCurrentProjectName();
        _currentProjectFolder = selectedProjectFolder;

        _projectNameLabel.Text = selectedProjectName is null
            ? "No project selected."
            : $"Current project: {selectedProjectName}";

        _createDatasetContent.SetProject(selectedProjectFolder);
        _createCohortContent.SetProject(selectedProjectFolder);
        _createAnalysisContent.SetProject(selectedProjectFolder);
        _createFigureContent.SetProject(selectedProjectFolder);
        _contentArea.Content = _placeholderContent;
        _datasetNames = ImmutableArray<string>.Empty;
        _cohortNames = ImmutableArray<string>.Empty;
        _analysisNames = ImmutableArray<string>.Empty;
        _figureNames = ImmutableArray<string>.Empty;

        if (selectedProjectFolder is null)
        {
            _placeholderLabel.Text = "No project selected.";
            RebuildNavigationButtons();
            return;
        }

        _placeholderLabel.Text = "Loading datasets, cohorts, and analyses...";
        RebuildNavigationButtons();
        MessageRouter.SendMessage(new DatasetListRequest(selectedProjectFolder));
        MessageRouter.SendMessage(new CohortListRequest(selectedProjectFolder));
        MessageRouter.SendMessage(new AnalysisListRequest(selectedProjectFolder));
        MessageRouter.SendMessage(new FigureListRequest(selectedProjectFolder));
    }

    internal void OnDatasetListResponseReceived(DatasetListResponse response)
    {
        // GatewayToTaskManager guarantees that incoming messages are dispatched on the UI thread.
        if (!string.Equals(response.ProjectFolder, _currentProjectFolder, StringComparison.Ordinal))
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
        if (!string.Equals(response.ProjectFolder, _currentProjectFolder, StringComparison.Ordinal))
        {
            return;
        }

        _createDatasetContent.OnCreateDatasetResponseReceived(response);

        if (response.Success)
        {
            MessageRouter.SendMessage(new DatasetListRequest(response.ProjectFolder!));
        }
    }

    internal void OnCohortListResponseReceived(CohortListResponse response)
    {
        // GatewayToTaskManager guarantees that incoming messages are dispatched on the UI thread.
        if (!string.Equals(response.ProjectFolder, _currentProjectFolder, StringComparison.Ordinal))
        {
            return;
        }

        _cohortNames = response.CohortNames;
        RebuildNavigationButtons();
    }

    internal void OnCreateCohortResponseReceived(CreateCohortResponse response)
    {
        // GatewayToTaskManager guarantees that incoming messages are dispatched on the UI thread.
        if (!string.Equals(response.ProjectFolder, _currentProjectFolder, StringComparison.Ordinal))
        {
            return;
        }

        _createCohortContent.OnCreateCohortResponseReceived(response);

        if (response.Success)
        {
            MessageRouter.SendMessage(new CohortListRequest(response.ProjectFolder!));
        }
    }

    internal void OnAnalysisListResponseReceived(AnalysisListResponse response)
    {
        // GatewayToTaskManager guarantees that incoming messages are dispatched on the UI thread.
        if (!string.Equals(response.ProjectFolder, _currentProjectFolder, StringComparison.Ordinal))
        {
            return;
        }

        _analysisNames = response.AnalysisNames;
        RebuildNavigationButtons();
    }

    internal void OnCreateAnalysisResponseReceived(CreateAnalysisResponse response)
    {
        // GatewayToTaskManager guarantees that incoming messages are dispatched on the UI thread.
        if (!string.Equals(response.ProjectFolder, _currentProjectFolder, StringComparison.Ordinal))
        {
            return;
        }

        _createAnalysisContent.OnCreateAnalysisResponseReceived(response);

        if (response.Success)
        {
            MessageRouter.SendMessage(new AnalysisListRequest(response.ProjectFolder!));
        }
    }

    internal void OnFigureListResponseReceived(FigureListResponse response)
    {
        // GatewayToTaskManager guarantees that incoming messages are dispatched on the UI thread.
        if (!string.Equals(response.ProjectFolder, _currentProjectFolder, StringComparison.Ordinal))
        {
            return;
        }

        _figureNames = response.FigureNames;
        RebuildNavigationButtons();
    }

    internal void OnCreateFigureResponseReceived(CreateFigureResponse response)
    {
        // GatewayToTaskManager guarantees that incoming messages are dispatched on the UI thread.
        if (!string.Equals(response.ProjectFolder, _currentProjectFolder, StringComparison.Ordinal))
        {
            return;
        }

        _createFigureContent.OnCreateFigureResponseReceived(response);

        if (response.Success)
        {
            MessageRouter.SendMessage(new FigureListRequest(response.ProjectFolder!));
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
                _datasetContent.SetDataset(_currentProjectFolder!, datasetName);
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
                _cohortContent.SetCohort(_currentProjectFolder!, cohortName);
                _contentArea.Content = _cohortContent;
            };
            _navigationPanel.Children.Add(cohortButton);
        }

        _navigationPanel.Children.Add(new TextBlock { Text = "Analyses", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 12, 0, 0) });
        _navigationPanel.Children.Add(_createAnalysisButton);

        foreach (var analysisName in _analysisNames)
        {
            var analysisButton = new Button { Content = analysisName };
            analysisButton.Click += (_, _) =>
            {
                _analysisContent.SetAnalysis(_currentProjectFolder!, analysisName);
                _contentArea.Content = _analysisContent;
            };
            _navigationPanel.Children.Add(analysisButton);
        }

        _navigationPanel.Children.Add(new TextBlock { Text = "Figures", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 12, 0, 0) });
        _navigationPanel.Children.Add(_createFigureButton);

        foreach (var figureName in _figureNames)
        {
            var figureButton = new Button { Content = figureName };
            figureButton.Click += (_, _) =>
            {
                _figureContent.SetFigure(_currentProjectFolder!, figureName);
                _contentArea.Content = _figureContent;
            };
            _navigationPanel.Children.Add(figureButton);
        }
    }

    private static Brush? GetThemeBrush(string resourceKey)
    {
        return Application.Current.Resources.TryGetValue(resourceKey, out var resource)
            ? resource as Brush
            : null;
    }
}
