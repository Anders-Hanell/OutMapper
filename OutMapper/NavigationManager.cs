namespace OutMapper;

internal sealed class NavigationManager
{
    private readonly ContentControl _contentControl;
    private readonly UIElement _settingsContent;
    private readonly ProjectsPanel _projectsPanel;

    public NavigationManager(
        ContentControl contentControl,
        UIElement settingsContent,
        ProjectsPanel projectsPanel)
    {
        _contentControl = contentControl;
        _settingsContent = settingsContent;
        _projectsPanel = projectsPanel;
    }

    public void ShowSettings()
    {
        _contentControl.Content = _settingsContent;
    }

    public void ShowProjects()
    {
        _projectsPanel.Refresh();
        _contentControl.Content = _projectsPanel;
    }
}
