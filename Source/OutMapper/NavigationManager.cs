namespace OutMapper;

/// <summary>
/// Something that can refresh its own data before being shown. Kept separate from <see cref="ProjectsPanel"/>
/// so <see cref="NavigationManager"/> doesn't need a live Uno control to be unit tested.
/// </summary>
internal interface IRefreshable
{
    void Refresh();
}

internal sealed class NavigationManager
{
    private readonly IContentHost _contentHost;
    private readonly object _settingsContent;
    private readonly object _projectsContent;
    private readonly IRefreshable _projectsPanel;

    public NavigationManager(
        IContentHost contentHost,
        object settingsContent,
        object projectsContent,
        IRefreshable projectsPanel)
    {
        _contentHost = contentHost;
        _settingsContent = settingsContent;
        _projectsContent = projectsContent;
        _projectsPanel = projectsPanel;
    }

    public void ShowSettings()
    {
        _contentHost.Content = _settingsContent;
    }

    public void ShowProjects()
    {
        _projectsPanel.Refresh();
        _contentHost.Content = _projectsContent;
    }
}
