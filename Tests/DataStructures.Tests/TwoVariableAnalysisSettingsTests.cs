namespace DataStructures.Tests;

public class TwoVariableAnalysisSettingsTests
{
    private static NumericGridDef Grid(string channelName, double lowerLimit, double upperLimit, double binSize)
    {
        return NumericGridDef.Create(channelName, lowerLimit, upperLimit, binSize)
            .Should().BeOfType<Success<NumericGridDef>>().Subject.Value;
    }

    [Fact]
    public void Create_succeeds_with_no_warning_when_ranges_are_whole_numbers_of_bins()
    {
        var channelAGrid = Grid("ICP", 0, 10, 5);
        var channelBGrid = Grid("PRx", -1, 1, 0.5);

        var result = TwoVariableAnalysisSettings.Create("Cohort1", channelAGrid, channelBGrid);

        var settings = result.Should().BeOfType<Success<TwoVariableAnalysisSettings>>().Subject.Value;
        settings.RangeWarning.Should().BeNull();
    }

    [Fact]
    public void Create_warns_when_a_channels_range_is_not_a_whole_number_of_bins()
    {
        var channelAGrid = Grid("ICP", 0, 12, 5);
        var channelBGrid = Grid("PRx", -1, 1, 0.5);

        var result = TwoVariableAnalysisSettings.Create("Cohort1", channelAGrid, channelBGrid);

        var settings = result.Should().BeOfType<Success<TwoVariableAnalysisSettings>>().Subject.Value;
        settings.RangeWarning.Should().Contain("first channel");
    }

    [Fact]
    public void Create_fails_when_the_cohort_name_is_blank()
    {
        var channelAGrid = Grid("ICP", 0, 10, 5);
        var channelBGrid = Grid("PRx", -1, 1, 0.5);

        var result = TwoVariableAnalysisSettings.Create("   ", channelAGrid, channelBGrid);

        result.Should().BeOfType<Failure<TwoVariableAnalysisSettings>>();
    }

    [Fact]
    public void Create_fails_when_the_two_channels_have_the_same_name()
    {
        var channelAGrid = Grid("ICP", 0, 10, 5);
        var channelBGrid = Grid("ICP", -1, 1, 0.5);

        var result = TwoVariableAnalysisSettings.Create("Cohort1", channelAGrid, channelBGrid);

        result.Should().BeOfType<Failure<TwoVariableAnalysisSettings>>();
    }

    [Fact]
    public void ToByteArray_and_FromByteArray_round_trips_the_settings()
    {
        var channelAGrid = Grid("ICP", 0, 10, 5);
        var channelBGrid = NumericGridDef.Create("PRx", -1, 1, 0.5, isLeftInclusive: false)
            .Should().BeOfType<Success<NumericGridDef>>().Subject.Value;
        var original = TwoVariableAnalysisSettings.Create("Cohort1", channelAGrid, channelBGrid)
            .Should().BeOfType<Success<TwoVariableAnalysisSettings>>().Subject.Value;

        var roundTripped = TwoVariableAnalysisSettings.FromByteArray(original.ToByteArray())
            .Should().BeOfType<Success<TwoVariableAnalysisSettings>>().Subject.Value;

        roundTripped.CohortName.Should().Be(original.CohortName);
        roundTripped.ChannelAGrid.ChannelName.Should().Be(original.ChannelAGrid.ChannelName);
        roundTripped.ChannelAGrid.IsLeftInclusive.Should().Be(original.ChannelAGrid.IsLeftInclusive);
        roundTripped.ChannelBGrid.ChannelName.Should().Be(original.ChannelBGrid.ChannelName);
        roundTripped.ChannelBGrid.IsLeftInclusive.Should().Be(original.ChannelBGrid.IsLeftInclusive);
    }
}
