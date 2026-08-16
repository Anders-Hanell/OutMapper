using Uno.Extensions.Markup;

namespace OutMapper;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        var contentGrid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
            }
        };

        var settingsContent = new SettingsPanel();
        var projectsPanel = new ProjectsPanel();

        var contentControl = new ContentControl
        {
            Content = settingsContent
        };

        var navigationManager = new NavigationManager(
            new ContentControlHost(contentControl),
            settingsContent,
            projectsPanel,
            projectsPanel);

        var navigationPanel = new MainNavigationPanel(navigationManager);

        Grid.SetRow(contentControl, 1);

        contentGrid.Children.Add(navigationPanel);
        contentGrid.Children.Add(contentControl);

        this.Background(ThemeResource.Get<Brush>("ApplicationPageBackgroundThemeBrush"));
        this.Content = contentGrid;
    }
}
