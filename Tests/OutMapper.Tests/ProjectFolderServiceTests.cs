using TestSupport;
using Path = System.IO.Path;

namespace OutMapper.Tests;

public class ProjectFolderServiceTests
{
    private const string WorkspaceFolder = "/workspace";

    [Fact]
    public void TryCreateProject_creates_the_project_and_its_internal_folders()
    {
        var fileSystem = new InMemoryFileSystem();
        fileSystem.CreateDirectory(WorkspaceFolder);

        var created = ProjectFolderService.TryCreateProject(fileSystem, WorkspaceFolder, "MyProject", out var message);

        created.Should().BeTrue();
        message.Should().Contain("MyProject");
        fileSystem.DirectoryExists(Path.Combine(WorkspaceFolder, "Projects", "MyProject")).Should().BeTrue();
        fileSystem.DirectoryExists(Path.Combine(WorkspaceFolder, "Projects", "MyProject", ProjectFolderService.InternalFilesFolderName))
            .Should().BeTrue();
        fileSystem.DirectoryExists(Path.Combine(WorkspaceFolder, "Projects", "MyProject", ProjectFolderService.ProjectOutputFolderName))
            .Should().BeTrue();
    }

    [Fact]
    public void TryCreateProject_fails_without_a_valid_workspace()
    {
        var fileSystem = new InMemoryFileSystem();

        var created = ProjectFolderService.TryCreateProject(fileSystem, workspaceFolder: null, "MyProject", out var message);

        created.Should().BeFalse();
        message.Should().Contain("workspace");
    }

    [Theory]
    [InlineData("")]
    [InlineData("con")]
    [InlineData("bad/name")]
    [InlineData("trailing.")]
    public void TryCreateProject_rejects_invalid_project_names(string proposedName)
    {
        var fileSystem = new InMemoryFileSystem();
        fileSystem.CreateDirectory(WorkspaceFolder);

        var created = ProjectFolderService.TryCreateProject(fileSystem, WorkspaceFolder, proposedName, out _);

        created.Should().BeFalse();
    }

    [Fact]
    public void TryCreateProject_fails_when_a_project_with_the_same_name_already_exists()
    {
        var fileSystem = new InMemoryFileSystem();
        fileSystem.CreateDirectory(WorkspaceFolder);
        ProjectFolderService.TryCreateProject(fileSystem, WorkspaceFolder, "MyProject", out _);

        var createdAgain = ProjectFolderService.TryCreateProject(fileSystem, WorkspaceFolder, "MyProject", out var message);

        createdAgain.Should().BeFalse();
        message.Should().Contain("already exists");
    }

    [Fact]
    public void TrySelectProject_then_GetSelectedProjectName_round_trips_through_an_in_memory_settings_store()
    {
        var fileSystem = new InMemoryFileSystem();
        var settingsStore = new InMemorySettingsStore();
        fileSystem.CreateDirectory(WorkspaceFolder);
        ProjectFolderService.TryCreateProject(fileSystem, WorkspaceFolder, "MyProject", out _);

        var selected = ProjectFolderService.TrySelectProject(fileSystem, settingsStore, WorkspaceFolder, "MyProject", out _);

        selected.Should().BeTrue();
        ProjectFolderService.GetSelectedProjectName(fileSystem, settingsStore, WorkspaceFolder, out var error).Should().Be("MyProject");
        error.Should().BeNull();
    }

    [Fact]
    public void GetSelectedProjectName_clears_the_selection_when_the_project_folder_no_longer_exists()
    {
        var fileSystem = new InMemoryFileSystem();
        var settingsStore = new InMemorySettingsStore();
        fileSystem.CreateDirectory(WorkspaceFolder);
        ProjectFolderService.TryCreateProject(fileSystem, WorkspaceFolder, "MyProject", out _);
        ProjectFolderService.TrySelectProject(fileSystem, settingsStore, WorkspaceFolder, "MyProject", out _);

        // Simulate the project folder having been deleted outside the app.
        var deletedFileSystem = new InMemoryFileSystem();
        deletedFileSystem.CreateDirectory(WorkspaceFolder);

        var selectedProject = ProjectFolderService.GetSelectedProjectName(deletedFileSystem, settingsStore, WorkspaceFolder, out var error);

        selectedProject.Should().BeNull();
        error.Should().Contain("no longer exists");
    }
}
