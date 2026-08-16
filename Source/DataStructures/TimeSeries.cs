using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;

namespace DataStructures;

public sealed class TimeSeries
{
    public const float MissingValue = float.MinValue;

    private sealed class DataTransferObject
    {
        public List<long> TimestampTicks { get; set; } = new();
        public Dictionary<string, List<float>> Channels { get; set; } = new();
    }

    private TimeSeries(ImmutableArray<DateTime> timestamps, ImmutableDictionary<string, ImmutableArray<float>> channels)
    {
        // Private constructor to make sure object creation goes through Create().
        Timestamps = timestamps;
        Channels = channels;
    }

    public ImmutableArray<DateTime> Timestamps { get; }
    public ImmutableDictionary<string, ImmutableArray<float>> Channels { get; }

    public static Result<TimeSeries> Create(
        IReadOnlyList<DateTime> timestamps, IReadOnlyDictionary<string, IReadOnlyList<float>> channels)
    {
        if (timestamps.Count == 0)
        {
            return new Failure<TimeSeries>("A time series must have at least one timestamp.");
        }

        if (channels.Count == 0)
        {
            return new Failure<TimeSeries>("A time series must have at least one channel.");
        }

        foreach (var (channelName, values) in channels)
        {
            if (values.Count != timestamps.Count)
            {
                return new Failure<TimeSeries>(
                    $"Channel '{channelName}' has {values.Count} value(s) but there are {timestamps.Count} timestamp(s).");
            }
        }

        for (var i = 1; i < timestamps.Count; i++)
        {
            if (timestamps[i] <= timestamps[i - 1])
            {
                return new Failure<TimeSeries>(
                    $"Timestamps must be strictly increasing and unique. Timestamp at index {i} ({timestamps[i]:o}) " +
                    $"is not greater than the previous timestamp ({timestamps[i - 1]:o}).");
            }
        }

        foreach (var (channelName, values) in channels)
        {
            foreach (var value in values)
            {
                if (value != MissingValue && (float.IsNaN(value) || float.IsInfinity(value)))
                {
                    return new Failure<TimeSeries>($"Channel '{channelName}' contains an invalid value ({value}).");
                }
            }
        }

        var immutableChannels = channels.ToImmutableDictionary(
            pair => pair.Key,
            pair => pair.Value.ToImmutableArray());

        return new Success<TimeSeries>(new TimeSeries(timestamps.ToImmutableArray(), immutableChannels));
    }

    public List<byte> ToByteArray()
    {
        var dto = new DataTransferObject
        {
            TimestampTicks = Timestamps.Select(timestamp => timestamp.Ticks).ToList(),
            Channels = Channels.ToDictionary(pair => pair.Key, pair => pair.Value.ToList())
        };

        return JsonSerializer.SerializeToUtf8Bytes(dto).ToList();
    }

    public static Result<TimeSeries> FromByteArray(List<byte> bytes)
    {
        DataTransferObject? dto;
        try
        {
            dto = JsonSerializer.Deserialize<DataTransferObject>(bytes.ToArray());
        }
        catch (JsonException exception)
        {
            return new Failure<TimeSeries>($"Could not deserialize time series data: {exception.Message}");
        }

        if (dto is null)
        {
            return new Failure<TimeSeries>("Could not deserialize time series data: content was empty.");
        }

        var timestamps = dto.TimestampTicks.Select(ticks => new DateTime(ticks)).ToList();
        var channels = dto.Channels.ToDictionary(
            pair => pair.Key,
            IReadOnlyList<float> (pair) => pair.Value);

        return Create(timestamps, channels);
    }
}
