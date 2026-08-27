namespace OutMapper;

internal sealed class MainPanel : Grid
{
    public MainPanel()
    {
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

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

        Children.Add(navigationPanel);
        Children.Add(contentControl);
    }
}
