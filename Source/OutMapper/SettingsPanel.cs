namespace OutMapper;

internal sealed class SettingsPanel : Grid
{
    public SettingsPanel()
    {
        Padding = new Thickness(16);
        ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var settingsUsageContent = new Border
        {
            Padding = new Thickness(16),
            Child = new TextBlock
            {
                Text = "Settings - Usage view",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 20
            }
        };

        var settingsRecentProjectsContent = new SettingsRecentProjectsContent();
        var settingsCreateProjectContent = new SettingsCreateProjectContent();
        var settingsMultitaskingContent = new SettingsMultitaskingContent();

        var settingsInnerContentControl = new ContentControl
        {
            Content = settingsUsageContent
        };

        var settingsSidebar = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            Padding = new Thickness(8)
        };

        var settingsUsageButton = new Button { Content = "Usage" };
        var settingsRecentProjectsButton = new Button { Content = "Recent Projects" };
        var settingsCreateProjectButton = new Button { Content = "Create Project" };
        var settingsMultitaskingButton = new Button { Content = "Multitasking" };

        settingsUsageButton.Click += (_, _) => settingsInnerContentControl.Content = settingsUsageContent;
        settingsRecentProjectsButton.Click += (_, _) =>
        {
            settingsRecentProjectsContent.Refresh();
            settingsInnerContentControl.Content = settingsRecentProjectsContent;
        };
        settingsCreateProjectButton.Click += (_, _) =>
        {
            settingsCreateProjectContent.Reset();
            settingsInnerContentControl.Content = settingsCreateProjectContent;
        };
        settingsMultitaskingButton.Click += (_, _) => settingsInnerContentControl.Content = settingsMultitaskingContent;

        settingsSidebar.Children.Add(settingsUsageButton);
        settingsSidebar.Children.Add(settingsRecentProjectsButton);
        settingsSidebar.Children.Add(settingsCreateProjectButton);
        settingsSidebar.Children.Add(settingsMultitaskingButton);

        Grid.SetColumn(settingsInnerContentControl, 1);

        Children.Add(settingsSidebar);
        Children.Add(settingsInnerContentControl);
    }
}
