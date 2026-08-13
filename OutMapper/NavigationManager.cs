namespace OutMapper;

internal sealed class NavigationManager
{
    private readonly ContentControl _contentControl;
    private readonly UIElement _settingsContent;
    private readonly UIElement _projectsContent;
    private readonly TextBlock _projectsStatusLabel;
    private readonly ProjectDatasetsContent _projectDatasetsContent;

    public NavigationManager(
        ContentControl contentControl,
        UIElement settingsContent,
        UIElement projectsContent,
        TextBlock projectsStatusLabel,
        ProjectDatasetsContent projectDatasetsContent)
    {
        _contentControl = contentControl;
        _settingsContent = settingsContent;
        _projectsContent = projectsContent;
        _projectsStatusLabel = projectsStatusLabel;
        _projectDatasetsContent = projectDatasetsContent;
    }

    public void ShowSettings()
    {
        _contentControl.Content = _settingsContent;
    }

    public void ShowProjects()
    {
        var selectedProject = ProjectFolderService.GetSelectedProjectName(out var error);
        _projectsStatusLabel.Text = error ?? (selectedProject is null
            ? "No project selected."
            : $"Current project: {selectedProject}");
        _projectDatasetsContent.Refresh(selectedProject);
        _contentControl.Content = _projectsContent;
    }
}
