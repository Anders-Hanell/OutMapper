using TestSupport;

namespace OutMapper.Tests;

public class RecentProjectsServiceTests
{
    [Fact]
    public void AddOrPromote_adds_a_new_entry_to_the_front()
    {
        var fileSystem = new InMemoryFileSystem();
        var settingsStore = new InMemorySettingsStore();
        fileSystem.CreateDirectory("/projects/A");
        fileSystem.CreateDirectory("/projects/B");

        RecentProjectsService.AddOrPromote(settingsStore, "/projects/A");
        RecentProjectsService.AddOrPromote(settingsStore, "/projects/B");

        var entries = RecentProjectsService.GetRecentProjects(fileSystem, settingsStore);

        entries.Select(entry => entry.Folder).Should().Equal("/projects/B", "/projects/A");
    }

    [Fact]
    public void AddOrPromote_moves_an_existing_entry_to_the_front_instead_of_duplicating_it()
    {
        var fileSystem = new InMemoryFileSystem();
        var settingsStore = new InMemorySettingsStore();
        fileSystem.CreateDirectory("/projects/A");
        fileSystem.CreateDirectory("/projects/B");

        RecentProjectsService.AddOrPromote(settingsStore, "/projects/A");
        RecentProjectsService.AddOrPromote(settingsStore, "/projects/B");
        RecentProjectsService.AddOrPromote(settingsStore, "/projects/A");

        var entries = RecentProjectsService.GetRecentProjects(fileSystem, settingsStore);

        entries.Select(entry => entry.Folder).Should().Equal("/projects/A", "/projects/B");
    }

    [Fact]
    public void AddOrPromote_caps_the_list_at_ten_entries()
    {
        var fileSystem = new InMemoryFileSystem();
        var settingsStore = new InMemorySettingsStore();

        for (var i = 0; i < 12; i++)
        {
            var folder = $"/projects/Project{i}";
            fileSystem.CreateDirectory(folder);
            RecentProjectsService.AddOrPromote(settingsStore, folder);
        }

        var entries = RecentProjectsService.GetRecentProjects(fileSystem, settingsStore);

        entries.Should().HaveCount(10);
        entries[0].Folder.Should().Be("/projects/Project11");
        entries[^1].Folder.Should().Be("/projects/Project2");
    }

    [Fact]
    public void Remove_deletes_an_entry()
    {
        var fileSystem = new InMemoryFileSystem();
        var settingsStore = new InMemorySettingsStore();
        fileSystem.CreateDirectory("/projects/A");
        fileSystem.CreateDirectory("/projects/B");
        RecentProjectsService.AddOrPromote(settingsStore, "/projects/A");
        RecentProjectsService.AddOrPromote(settingsStore, "/projects/B");

        RecentProjectsService.Remove(settingsStore, "/projects/A");

        var entries = RecentProjectsService.GetRecentProjects(fileSystem, settingsStore);
        entries.Select(entry => entry.Folder).Should().Equal("/projects/B");
    }

    [Fact]
    public void GetRecentProjects_marks_an_entry_as_missing_without_dropping_it_from_the_list()
    {
        var fileSystem = new InMemoryFileSystem();
        var settingsStore = new InMemorySettingsStore();
        // Note: the folder is never created in fileSystem, simulating deletion outside the app.
        RecentProjectsService.AddOrPromote(settingsStore, "/projects/Deleted");

        var entries = RecentProjectsService.GetRecentProjects(fileSystem, settingsStore);

        entries.Should().HaveCount(1);
        entries[0].Folder.Should().Be("/projects/Deleted");
        entries[0].Name.Should().Be("Deleted");
        entries[0].IsMissing.Should().BeTrue();
    }
}
