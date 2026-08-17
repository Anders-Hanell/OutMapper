using DataStructures;
using TestSupport;
using Path = System.IO.Path;

namespace TaskManager.Tests;

public class AnalysisServiceTests
{
    private const string ProjectFolder = "/projects/MyProject";

    private static NumericGridDef Grid(string channelName, double lowerLimit, double upperLimit, double binSize)
    {
        return NumericGridDef.Create(channelName, lowerLimit, upperLimit, binSize)
            .Should().BeOfType<Success<NumericGridDef>>().Subject.Value;
    }

    private static TwoVariableAnalysisSettings Settings(string cohortName)
    {
        var channelAGrid = Grid("ICP", 0, 10, 5);
        var channelBGrid = Grid("PRx", -1, 1, 0.5);

        return TwoVariableAnalysisSettings.Create(cohortName, channelAGrid, channelBGrid)
            .Should().BeOfType<Success<TwoVariableAnalysisSettings>>().Subject.Value;
    }

    [Fact]
    public void ReadPersistedSettings_returns_not_found_when_nothing_was_ever_written()
    {
        var fileSystem = new InMemoryFileSystem();
        fileSystem.CreateDirectory(ProjectFolder);

        var response = AnalysisService.ReadPersistedSettings(fileSystem, ProjectFolder, "MyAnalysis");

        response.Found.Should().BeFalse();
        response.Settings.Should().BeNull();
    }

    [Fact]
    public async Task GenerateGraphAsync_persists_settings_even_when_generation_fails()
    {
        var fileSystem = new InMemoryFileSystem();
        fileSystem.CreateDirectory(ProjectFolder);
        var settings = Settings("Cohort1");

        var response = await AnalysisService.GenerateGraphAsync(fileSystem, ProjectFolder, "MyAnalysis", settings);

        response.Success.Should().BeFalse();
        var persisted = AnalysisService.ReadPersistedSettings(fileSystem, ProjectFolder, "MyAnalysis");
        persisted.Found.Should().BeTrue();
        persisted.Settings!.CohortName.Should().Be("Cohort1");
        persisted.Settings.ChannelAGrid.ChannelName.Should().Be("ICP");
        persisted.Settings.ChannelBGrid.ChannelName.Should().Be("PRx");
    }

    [Fact]
    public async Task DiscoverChannelNamesAsync_returns_the_sorted_union_of_channel_names_across_linked_datasets()
    {
        var fileSystem = new InMemoryFileSystem();
        var cohortFolder = Path.Combine(ProjectFolder, "OutMapper_InternalFiles", "Cohorts", "Cohort1");
        fileSystem.WriteAllBytes(
            Path.Combine(cohortFolder, "linked-datasets.json"),
            System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new[] { "Dataset1", "Dataset2" }));

        var timestamps = new List<DateTime> { new(2020, 1, 1) };

        var dataset1Series = TimeSeries.Create(timestamps, new Dictionary<string, IReadOnlyList<float>>
        {
            ["ICP"] = new List<float> { 1f },
            ["PRx"] = new List<float> { 0.1f }
        }).Should().BeOfType<Success<TimeSeries>>().Subject.Value;

        var dataset2Series = TimeSeries.Create(timestamps, new Dictionary<string, IReadOnlyList<float>>
        {
            ["ICP"] = new List<float> { 2f },
            ["ABP"] = new List<float> { 80f }
        }).Should().BeOfType<Success<TimeSeries>>().Subject.Value;

        fileSystem.WriteAllBytes(
            Path.Combine(ProjectFolder, "OutMapper_InternalFiles", "Datasets", "Dataset1", "Parsed data", "patient1.json"),
            dataset1Series.ToByteArray().ToArray());
        fileSystem.WriteAllBytes(
            Path.Combine(ProjectFolder, "OutMapper_InternalFiles", "Datasets", "Dataset2", "Parsed data", "patient1.json"),
            dataset2Series.ToByteArray().ToArray());

        var channelNames = await AnalysisService.DiscoverChannelNamesAsync(fileSystem, ProjectFolder, "Cohort1");

        channelNames.Should().Equal("ABP", "ICP", "PRx");
    }

    [Fact]
    public async Task DiscoverChannelNamesAsync_returns_empty_when_the_cohort_has_no_linked_datasets()
    {
        var fileSystem = new InMemoryFileSystem();
        fileSystem.CreateDirectory(Path.Combine(ProjectFolder, "OutMapper_InternalFiles", "Cohorts", "Cohort1"));

        var channelNames = await AnalysisService.DiscoverChannelNamesAsync(fileSystem, ProjectFolder, "Cohort1");

        channelNames.Should().BeEmpty();
    }
}
