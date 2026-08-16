using TestSupport;
using Path = System.IO.Path;

namespace OutMapper.Tests;

public class ProjectFolderServiceTests
{
    private const string ParentFolder = "/parent";

    [Fact]
    public void TryCreateProject_creates_the_project_and_its_internal_folders_and_auto_selects_it()
    {
        var fileSystem = new InMemoryFileSystem();
        var settingsStore = new InMemorySettingsStore();
        fileSystem.CreateDirectory(ParentFolder);

        var created = ProjectFolderService.TryCreateProject(fileSystem, settingsStore, ParentFolder, "MyProject", out var message);

        var projectFolder = Path.Combine(ParentFolder, "MyProject");
        created.Should().BeTrue();
        message.Should().Contain("MyProject");
        fileSystem.DirectoryExists(projectFolder).Should().BeTrue();
        fileSystem.DirectoryExists(Path.Combine(projectFolder, ProjectFolderService.InternalFilesFolderName)).Should().BeTrue();
        fileSystem.DirectoryExists(Path.Combine(projectFolder, ProjectFolderService.ProjectOutputFolderName)).Should().BeTrue();
        ProjectFolderService.GetCurrentProjectFolder(fileSystem, settingsStore).Should().Be(projectFolder);
    }

    [Fact]
    public void TryCreateProject_fails_without_a_valid_parent_folder()
    {
        var fileSystem = new InMemoryFileSystem();
        var settingsStore = new InMemorySettingsStore();

        var created = ProjectFolderService.TryCreateProject(fileSystem, settingsStore, parentFolder: null, "MyProject", out var message);

        created.Should().BeFalse();
        message.Should().Contain("location");
    }

    [Theory]
    [InlineData("")]
    [InlineData("con")]
    [InlineData("bad/name")]
    [InlineData("trailing.")]
    public void TryCreateProject_rejects_invalid_project_names(string proposedName)
    {
        var fileSystem = new InMemoryFileSystem();
        var settingsStore = new InMemorySettingsStore();
        fileSystem.CreateDirectory(ParentFolder);

        var created = ProjectFolderService.TryCreateProject(fileSystem, settingsStore, ParentFolder, proposedName, out _);

        created.Should().BeFalse();
    }

    [Fact]
    public void TryCreateProject_fails_when_a_project_with_the_same_name_already_exists_at_that_location()
    {
        var fileSystem = new InMemoryFileSystem();
        var settingsStore = new InMemorySettingsStore();
        fileSystem.CreateDirectory(ParentFolder);
        ProjectFolderService.TryCreateProject(fileSystem, settingsStore, ParentFolder, "MyProject", out _);

        var createdAgain = ProjectFolderService.TryCreateProject(fileSystem, settingsStore, ParentFolder, "MyProject", out var message);

        createdAgain.Should().BeFalse();
        message.Should().Contain("already exists");
    }

    [Fact]
    public void TryOpenProject_selects_a_folder_containing_OutMapper_InternalFiles()
    {
        var fileSystem = new InMemoryFileSystem();
        var settingsStore = new InMemorySettingsStore();
        fileSystem.CreateDirectory(ParentFolder);
        ProjectFolderService.TryCreateProject(fileSystem, settingsStore, ParentFolder, "MyProject", out _);
        var projectFolder = Path.Combine(ParentFolder, "MyProject");

        var otherSettingsStore = new InMemorySettingsStore();
        var opened = ProjectFolderService.TryOpenProject(fileSystem, otherSettingsStore, projectFolder, out var message);

        opened.Should().BeTrue();
        message.Should().Contain("MyProject");
        ProjectFolderService.GetCurrentProjectFolder(fileSystem, otherSettingsStore).Should().Be(projectFolder);
    }

    [Fact]
    public void TryOpenProject_rejects_a_folder_without_OutMapper_InternalFiles()
    {
        var fileSystem = new InMemoryFileSystem();
        var settingsStore = new InMemorySettingsStore();
        fileSystem.CreateDirectory(ParentFolder);

        var opened = ProjectFolderService.TryOpenProject(fileSystem, settingsStore, ParentFolder, out var message);

        opened.Should().BeFalse();
        message.Should().Contain("doesn't look like");
        ProjectFolderService.GetCurrentProjectFolder(fileSystem, settingsStore).Should().BeNull();
    }

    [Fact]
    public void TryOpenProject_rejects_a_nonexistent_folder()
    {
        var fileSystem = new InMemoryFileSystem();
        var settingsStore = new InMemorySettingsStore();

        var opened = ProjectFolderService.TryOpenProject(fileSystem, settingsStore, "/does-not-exist", out var message);

        opened.Should().BeFalse();
        message.Should().Contain("valid project folder");
    }

    [Fact]
    public void GetCurrentProjectFolder_self_heals_when_the_folder_no_longer_exists()
    {
        var fileSystem = new InMemoryFileSystem();
        var settingsStore = new InMemorySettingsStore();
        fileSystem.CreateDirectory(ParentFolder);
        ProjectFolderService.TryCreateProject(fileSystem, settingsStore, ParentFolder, "MyProject", out _);

        // Simulate the project folder having been deleted outside the app.
        var deletedFileSystem = new InMemoryFileSystem();

        var currentFolder = ProjectFolderService.GetCurrentProjectFolder(deletedFileSystem, settingsStore);

        currentFolder.Should().BeNull();

        // The self-heal should have cleared the stale setting, so a fresh check keeps returning null.
        ProjectFolderService.GetCurrentProjectFolder(deletedFileSystem, settingsStore).Should().BeNull();
    }
}
