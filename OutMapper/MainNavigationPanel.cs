namespace OutMapper;

internal sealed class MainNavigationPanel : StackPanel
{
    public MainNavigationPanel(NavigationManager navigationManager)
    {
        Orientation = Orientation.Horizontal;
        HorizontalAlignment = HorizontalAlignment.Center;
        Spacing = 12;
        Padding = new Thickness(16);

        var settingsButton = new Button { Content = "Settings" };
        var projectsButton = new Button { Content = "Projects" };

        settingsButton.Click += (_, _) => navigationManager.ShowSettings();
        projectsButton.Click += (_, _) => navigationManager.ShowProjects();

        Children.Add(settingsButton);
        Children.Add(projectsButton);
    }
}
