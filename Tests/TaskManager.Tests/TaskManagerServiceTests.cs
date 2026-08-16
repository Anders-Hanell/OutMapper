using TestSupport;
using Path = System.IO.Path;

namespace TaskManager.Tests;

public class TaskManagerServiceTests
{
    private const string WorkspaceFolder = "/workspace";
    private const string ProjectName = "MyProject";

    [Fact]
    public void CreateDataset_then_LocateDatasets_round_trips_through_an_in_memory_file_system()
    {
        var fileSystem = new InMemoryFileSystem();
        fileSystem.CreateDirectory(Path.Combine(WorkspaceFolder, "Projects", ProjectName));

        var created = TaskManagerService.CreateDataset(fileSystem, WorkspaceFolder, ProjectName, "MyDataset", rawDataFolderPath: null);

        created.Should().BeTrue();
        TaskManagerService.LocateDatasets(fileSystem, WorkspaceFolder, ProjectName).Should().Equal("MyDataset");
    }

    [Fact]
    public void CreateDataset_fails_when_the_project_folder_does_not_exist()
    {
        var fileSystem = new InMemoryFileSystem();

        var created = TaskManagerService.CreateDataset(fileSystem, WorkspaceFolder, ProjectName, "MyDataset", rawDataFolderPath: null);

        created.Should().BeFalse();
    }

    [Fact]
    public void CreateDataset_fails_when_a_dataset_with_the_same_name_already_exists()
    {
        var fileSystem = new InMemoryFileSystem();
        fileSystem.CreateDirectory(Path.Combine(WorkspaceFolder, "Projects", ProjectName));
        TaskManagerService.CreateDataset(fileSystem, WorkspaceFolder, ProjectName, "MyDataset", rawDataFolderPath: null);

        var createdAgain = TaskManagerService.CreateDataset(fileSystem, WorkspaceFolder, ProjectName, "MyDataset", rawDataFolderPath: null);

        createdAgain.Should().BeFalse();
    }

    [Fact]
    public void CreateDataset_copies_csv_files_from_the_raw_data_folder()
    {
        var fileSystem = new InMemoryFileSystem();
        fileSystem.CreateDirectory(Path.Combine(WorkspaceFolder, "Projects", ProjectName));
        const string rawDataFolder = "/raw-data";
        fileSystem.WriteAllBytes(Path.Combine(rawDataFolder, "patient1.csv"), "time,value"u8.ToArray());
        fileSystem.WriteAllBytes(Path.Combine(rawDataFolder, "notes.txt"), "ignored"u8.ToArray());

        TaskManagerService.CreateDataset(fileSystem, WorkspaceFolder, ProjectName, "MyDataset", rawDataFolder);

        var importedFolder = Path.Combine(
            WorkspaceFolder, "Projects", ProjectName, "OutMapper_InternalFiles", "Datasets", "MyDataset", "Imported raw data");
        fileSystem.FileExists(Path.Combine(importedFolder, "patient1.csv")).Should().BeTrue();
        fileSystem.FileExists(Path.Combine(importedFolder, "notes.txt")).Should().BeFalse();
    }

    [Fact]
    public void LocateDatasets_returns_empty_when_the_project_has_no_datasets_yet()
    {
        var fileSystem = new InMemoryFileSystem();
        fileSystem.CreateDirectory(Path.Combine(WorkspaceFolder, "Projects", ProjectName));

        TaskManagerService.LocateDatasets(fileSystem, WorkspaceFolder, ProjectName).Should().BeEmpty();
    }
}
