namespace DataStructures.Tests;

public class TimeSeriesTests
{
    [Fact]
    public void ToByteArray_and_FromByteArray_round_trips_the_time_series()
    {
        var timestamps = new List<DateTime>
        {
            new(2026, 1, 1, 0, 0, 0),
            new(2026, 1, 1, 0, 5, 0),
            new(2026, 1, 1, 0, 10, 0),
        };
        var channels = new Dictionary<string, IReadOnlyList<float>>
        {
            ["HR"] = new List<float> { 70f, 72f, TimeSeries.MissingValue },
            ["SpO2"] = new List<float> { 98f, 97.5f, 99f },
        };

        var original = TimeSeries.Create(timestamps, channels).Should().BeOfType<Success<TimeSeries>>().Subject.Value;

        var bytes = original.ToByteArray();
        var deserialized = TimeSeries.FromByteArray(bytes).Should().BeOfType<Success<TimeSeries>>().Subject.Value;

        deserialized.Timestamps.Should().Equal(original.Timestamps);
        deserialized.Channels.Should().BeEquivalentTo(original.Channels);
    }
}
