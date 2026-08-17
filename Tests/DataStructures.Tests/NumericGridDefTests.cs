namespace DataStructures.Tests;

public class NumericGridDefTests
{
    [Fact]
    public void Create_succeeds_and_computes_bin_count_for_a_whole_number_of_bins()
    {
        var result = NumericGridDef.Create("ICP", lowerLimit: 0, upperLimit: 10, binSize: 5);

        var grid = result.Should().BeOfType<Success<NumericGridDef>>().Subject.Value;
        grid.BinCount.Should().Be(2);
        grid.HasPartialLastBin.Should().BeFalse();
    }

    [Fact]
    public void Create_flags_partial_last_bin_when_the_range_is_not_a_whole_number_of_bins()
    {
        var result = NumericGridDef.Create("ICP", lowerLimit: 0, upperLimit: 12, binSize: 5);

        var grid = result.Should().BeOfType<Success<NumericGridDef>>().Subject.Value;
        grid.BinCount.Should().Be(3);
        grid.HasPartialLastBin.Should().BeTrue();
    }

    [Fact]
    public void Create_defaults_to_left_inclusive_when_not_specified()
    {
        var result = NumericGridDef.Create("ICP", lowerLimit: 0, upperLimit: 10, binSize: 5);

        result.Should().BeOfType<Success<NumericGridDef>>().Subject.Value.IsLeftInclusive.Should().BeTrue();
    }

    [Fact]
    public void Create_fails_when_channel_name_is_blank()
    {
        var result = NumericGridDef.Create("   ", lowerLimit: 0, upperLimit: 10, binSize: 5);

        result.Should().BeOfType<Failure<NumericGridDef>>();
    }

    [Fact]
    public void Create_fails_when_the_range_end_is_not_greater_than_the_start()
    {
        var result = NumericGridDef.Create("ICP", lowerLimit: 10, upperLimit: 10, binSize: 5);

        result.Should().BeOfType<Failure<NumericGridDef>>();
    }

    [Fact]
    public void Create_fails_when_bin_size_is_not_positive()
    {
        var result = NumericGridDef.Create("ICP", lowerLimit: 0, upperLimit: 10, binSize: 0);

        result.Should().BeOfType<Failure<NumericGridDef>>();
    }

    [Fact]
    public void ToByteArray_and_FromByteArray_round_trips_the_grid_def()
    {
        var original = NumericGridDef.Create("PRx", lowerLimit: -1, upperLimit: 1, binSize: 0.5, isLeftInclusive: false)
            .Should().BeOfType<Success<NumericGridDef>>().Subject.Value;

        var roundTripped = NumericGridDef.FromByteArray(original.ToByteArray())
            .Should().BeOfType<Success<NumericGridDef>>().Subject.Value;

        roundTripped.ChannelName.Should().Be(original.ChannelName);
        roundTripped.LowerLimit.Should().Be(original.LowerLimit);
        roundTripped.UpperLimit.Should().Be(original.UpperLimit);
        roundTripped.BinSize.Should().Be(original.BinSize);
        roundTripped.IsLeftInclusive.Should().Be(original.IsLeftInclusive);
        roundTripped.BinCount.Should().Be(original.BinCount);
    }
}
