using Messages;
using Microsoft.UI.Text;

namespace OutMapper;

public sealed class ProjectFigureContent : Border
{
    internal static ProjectFigureContent? Current { get; private set; }

    private readonly TextBlock _nameLabel;
    private readonly ContentControl _innerContentArea;
    private readonly ProjectFigureSizeContent _sizeContent;
    private readonly ProjectFigureSelectGraphsContent _selectGraphsContent;
    private string? _currentProjectFolder;
    private string? _currentFigureName;

    public ProjectFigureContent()
    {
        Padding = new Thickness(24);
        Background = GetThemeBrush("SystemControlBackgroundChromeMediumLowBrush");
        CornerRadius = new CornerRadius(12);

        _nameLabel = new TextBlock
        {
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 16)
        };

        _sizeContent = new ProjectFigureSizeContent();
        _selectGraphsContent = new ProjectFigureSelectGraphsContent();

        _innerContentArea = new ContentControl
        {
            Content = _sizeContent
        };

        var innerSidebar = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            Padding = new Thickness(8)
        };

        var sizeButton = new Button { Content = "Size" };
        var selectGraphsButton = new Button { Content = "Select graphs" };

        sizeButton.Click += (_, _) => _innerContentArea.Content = _sizeContent;
        selectGraphsButton.Click += (_, _) => _innerContentArea.Content = _selectGraphsContent;

        innerSidebar.Children.Add(sizeButton);
        innerSidebar.Children.Add(selectGraphsButton);

        var innerGrid = new Grid();
        innerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        innerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(_innerContentArea, 1);
        innerGrid.Children.Add(innerSidebar);
        innerGrid.Children.Add(_innerContentArea);

        Child = new StackPanel
        {
            Children = { _nameLabel, innerGrid }
        };

        Current = this;
    }

    public void SetFigure(string projectName, string figureName)
    {
        _currentProjectFolder = projectName;
        _currentFigureName = figureName;
        _nameLabel.Text = figureName;
        _sizeContent.SetFigure(projectName, figureName);
        _selectGraphsContent.SetFigure(projectName, figureName);
        _innerContentArea.Content = _sizeContent;
    }

    internal void OnFigureLayoutResponseReceived(FigureLayoutResponse response)
    {
        // GatewayToTaskManager guarantees that incoming messages are dispatched on the UI thread.
        if (!string.Equals(response.ProjectFolder, _currentProjectFolder, StringComparison.Ordinal) ||
            !string.Equals(response.FigureName, _currentFigureName, StringComparison.Ordinal))
        {
            return;
        }

        _sizeContent.OnFigureLayoutResponseReceived(response);
        _selectGraphsContent.OnFigureLayoutResponseReceived(response);
    }

    internal void OnSaveFigureSizeResponseReceived(SaveFigureSizeResponse response)
    {
        // GatewayToTaskManager guarantees that incoming messages are dispatched on the UI thread.
        if (!string.Equals(response.ProjectFolder, _currentProjectFolder, StringComparison.Ordinal) ||
            !string.Equals(response.FigureName, _currentFigureName, StringComparison.Ordinal))
        {
            return;
        }

        _sizeContent.OnSaveFigureSizeResponseReceived(response);
        _selectGraphsContent.OnSaveFigureSizeResponseReceived(response);
    }

    internal void OnCreateFigureGraphResponseReceived(CreateFigureGraphResponse response)
    {
        // GatewayToTaskManager guarantees that incoming messages are dispatched on the UI thread.
        if (!string.Equals(response.ProjectFolder, _currentProjectFolder, StringComparison.Ordinal) ||
            !string.Equals(response.FigureName, _currentFigureName, StringComparison.Ordinal))
        {
            return;
        }

        _selectGraphsContent.OnCreateFigureGraphResponseReceived(response);
    }

    private static Brush? GetThemeBrush(string resourceKey)
    {
        return Application.Current.Resources.TryGetValue(resourceKey, out var resource)
            ? resource as Brush
            : null;
    }
}
