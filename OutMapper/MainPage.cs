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

        var settingsWorkspaceContent = new SettingsWorkspaceContent();
        var settingsProjectsContent = new SettingsProjectsContent();
        var settingsSelectProjectContent = new SettingsSelectProjectContent();
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
        var settingsWorkspaceButton = new Button { Content = "Workspace" };
        var settingsProjectsButton = new Button { Content = "Current Projects" };
        var settingsSelectProjectButton = new Button { Content = "Select Project" };
        var settingsCreateProjectButton = new Button { Content = "Create Project" };
        var settingsMultitaskingButton = new Button { Content = "Multitasking" };

        settingsUsageButton.Click += (_, _) => settingsInnerContentControl.Content = settingsUsageContent;
        settingsWorkspaceButton.Click += (_, _) => settingsInnerContentControl.Content = settingsWorkspaceContent;
        settingsProjectsButton.Click += (_, _) =>
        {
            settingsProjectsContent.Refresh();
            settingsInnerContentControl.Content = settingsProjectsContent;
        };
        settingsSelectProjectButton.Click += (_, _) =>
        {
            settingsSelectProjectContent.Refresh();
            settingsInnerContentControl.Content = settingsSelectProjectContent;
        };
        settingsCreateProjectButton.Click += (_, _) =>
        {
            settingsCreateProjectContent.Reset();
            settingsInnerContentControl.Content = settingsCreateProjectContent;
        };
        settingsMultitaskingButton.Click += (_, _) => settingsInnerContentControl.Content = settingsMultitaskingContent;

        settingsSidebar.Children.Add(settingsUsageButton);
        settingsSidebar.Children.Add(settingsWorkspaceButton);
        settingsSidebar.Children.Add(settingsProjectsButton);
        settingsSidebar.Children.Add(settingsSelectProjectButton);
        settingsSidebar.Children.Add(settingsCreateProjectButton);
        settingsSidebar.Children.Add(settingsMultitaskingButton);

        var settingsContent = new Border
        {
            Padding = new Thickness(16),
            Child = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                },
                Children =
                {
                    settingsSidebar,
                    settingsInnerContentControl
                }
            }
        };

        Grid.SetColumn(settingsInnerContentControl, 1);

        var projectDatasetsContent = new ProjectDatasetsContent();

        var projectsStatusLabel = new TextBlock
        {
            Text = "Projects view",
            HorizontalAlignment = HorizontalAlignment.Center,
            FontSize = 20,
            Margin = new Thickness(0, 0, 0, 16)
        };

        var generatePdfButton = new Button
        {
            Content = "Generate pdf",
            MinHeight = 44,
            MinWidth = 140,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 16)
        };
        generatePdfButton.Click += (_, _) => GraphPdfService.GeneratePdf();

        var projectsContent = new Border
        {
            Padding = new Thickness(16),
            Child = new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    projectsStatusLabel,
                    generatePdfButton,
                    projectDatasetsContent
                }
            }
        };

        var contentControl = new ContentControl
        {
            Content = settingsContent
        };

        var navigationManager = new NavigationManager(
            contentControl,
            settingsContent,
            projectsContent,
            projectsStatusLabel,
            projectDatasetsContent);

        var navigationPanel = new MainNavigationPanel(navigationManager);

        Grid.SetRow(contentControl, 1);

        contentGrid.Children.Add(navigationPanel);
        contentGrid.Children.Add(contentControl);

        this.Background(ThemeResource.Get<Brush>("ApplicationPageBackgroundThemeBrush"));
        this.Content = contentGrid;
    }
}
