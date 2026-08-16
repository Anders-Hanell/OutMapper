namespace OutMapper.Tests;

public class NavigationManagerTests
{
    private sealed class FakeContentHost : IContentHost
    {
        public object? Content { get; set; }
    }

    private sealed class FakeRefreshable : IRefreshable
    {
        public int RefreshCallCount { get; private set; }

        public void Refresh() => RefreshCallCount++;
    }

    [Fact]
    public void ShowSettings_sets_the_settings_content_on_the_host()
    {
        var host = new FakeContentHost();
        var settingsContent = new object();
        var projectsContent = new object();
        var projectsPanel = new FakeRefreshable();
        var navigationManager = new NavigationManager(host, settingsContent, projectsContent, projectsPanel);

        navigationManager.ShowSettings();

        host.Content.Should().BeSameAs(settingsContent);
    }

    [Fact]
    public void ShowProjects_refreshes_the_projects_panel_and_sets_the_projects_content_on_the_host()
    {
        var host = new FakeContentHost();
        var settingsContent = new object();
        var projectsContent = new object();
        var projectsPanel = new FakeRefreshable();
        var navigationManager = new NavigationManager(host, settingsContent, projectsContent, projectsPanel);

        navigationManager.ShowProjects();

        host.Content.Should().BeSameAs(projectsContent);
        projectsPanel.RefreshCallCount.Should().Be(1);
    }
}
