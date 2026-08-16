namespace DataStructures.Tests;

public class TwoVariableAnalysisSettingsTests
{
    [Fact]
    public void Create_succeeds_with_no_warning_when_ranges_are_whole_numbers_of_bins()
    {
        var result = TwoVariableAnalysisSettings.Create(
            "Cohort1",
            "ICP", channelARangeStart: 0, channelARangeEnd: 10, channelABinWidth: 5,
            "PRx", channelBRangeStart: -1, channelBRangeEnd: 1, channelBBinWidth: 0.5);

        var settings = result.Should().BeOfType<Success<TwoVariableAnalysisSettings>>().Subject.Value;
        settings.RangeWarning.Should().BeNull();
    }

    [Fact]
    public void Create_warns_when_a_channels_range_is_not_a_whole_number_of_bins()
    {
        var result = TwoVariableAnalysisSettings.Create(
            "Cohort1",
            "ICP", channelARangeStart: 0, channelARangeEnd: 12, channelABinWidth: 5,
            "PRx", channelBRangeStart: -1, channelBRangeEnd: 1, channelBBinWidth: 0.5);

        var settings = result.Should().BeOfType<Success<TwoVariableAnalysisSettings>>().Subject.Value;
        settings.RangeWarning.Should().Contain("first channel");
    }

    [Fact]
    public void Create_fails_when_the_range_end_is_not_greater_than_the_start()
    {
        var result = TwoVariableAnalysisSettings.Create(
            "Cohort1",
            "ICP", channelARangeStart: 10, channelARangeEnd: 10, channelABinWidth: 5,
            "PRx", channelBRangeStart: -1, channelBRangeEnd: 1, channelBBinWidth: 0.5);

        result.Should().BeOfType<Failure<TwoVariableAnalysisSettings>>();
    }

    [Fact]
    public void Create_fails_when_a_bin_width_is_not_positive()
    {
        var result = TwoVariableAnalysisSettings.Create(
            "Cohort1",
            "ICP", channelARangeStart: 0, channelARangeEnd: 10, channelABinWidth: 0,
            "PRx", channelBRangeStart: -1, channelBRangeEnd: 1, channelBBinWidth: 0.5);

        result.Should().BeOfType<Failure<TwoVariableAnalysisSettings>>();
    }
}
