using System.Collections.Generic;
using System.Text.Json;

namespace DataStructures;

public sealed class NumericGridDef
{
    private sealed class DataTransferObject
    {
        public string ChannelName { get; set; } = "";
        public double LowerLimit { get; set; }
        public double UpperLimit { get; set; }
        public double BinSize { get; set; }
        public bool IsLeftInclusive { get; set; } = true;
    }

    private NumericGridDef(
        string channelName, double lowerLimit, double upperLimit, double binSize, int binCount,
        bool isLeftInclusive, bool hasPartialLastBin)
    {
        // Private constructor to make sure object creation goes through Create().
        ChannelName = channelName;
        LowerLimit = lowerLimit;
        UpperLimit = upperLimit;
        BinSize = binSize;
        BinCount = binCount;
        IsLeftInclusive = isLeftInclusive;
        HasPartialLastBin = hasPartialLastBin;
    }

    public string ChannelName { get; }
    public double LowerLimit { get; }
    public double UpperLimit { get; }
    public double BinSize { get; }

    /// <summary>
    /// Number of bins spanning [LowerLimit, UpperLimit] at BinSize, derived the same way
    /// Algorithms.GridBinning.ComputeBinEdges derives it (ceiling of the span divided by the bin
    /// size, at least 1).
    /// </summary>
    public int BinCount { get; }

    /// <summary>
    /// True when each bin is closed on its lower edge and open on its upper edge, except the very
    /// last bin, which is closed on both ends; false for the mirror image (closed on the upper edge,
    /// open on the lower edge, except the very first bin).
    /// </summary>
    public bool IsLeftInclusive { get; }

    /// <summary>
    /// True when [LowerLimit, UpperLimit] is not a whole number of bins at BinSize, meaning the last
    /// bin will be narrower than the others. Does not block use of this grid definition.
    /// </summary>
    public bool HasPartialLastBin { get; }

    public static Result<NumericGridDef> Create(
        string channelName, double lowerLimit, double upperLimit, double binSize, bool isLeftInclusive = true)
    {
        if (string.IsNullOrWhiteSpace(channelName))
        {
            return new Failure<NumericGridDef>("Enter a channel name.");
        }

        if (!double.IsFinite(lowerLimit) || !double.IsFinite(upperLimit) || upperLimit <= lowerLimit)
        {
            return new Failure<NumericGridDef>("The end of the range must be greater than the start.");
        }

        if (!double.IsFinite(binSize) || binSize <= 0)
        {
            return new Failure<NumericGridDef>("The bin size must be a positive number.");
        }

        var rawBinCount = (upperLimit - lowerLimit) / binSize;
        var binCount = Math.Max(1, (int)Math.Ceiling(rawBinCount));
        var hasPartialLastBin = Math.Abs(rawBinCount - Math.Round(rawBinCount)) > 1e-9;

        return new Success<NumericGridDef>(
            new NumericGridDef(channelName, lowerLimit, upperLimit, binSize, binCount, isLeftInclusive, hasPartialLastBin));
    }

    public List<byte> ToByteArray()
    {
        var dto = new DataTransferObject
        {
            ChannelName = ChannelName,
            LowerLimit = LowerLimit,
            UpperLimit = UpperLimit,
            BinSize = BinSize,
            IsLeftInclusive = IsLeftInclusive
        };

        return JsonSerializer.SerializeToUtf8Bytes(dto).ToList();
    }

    public static Result<NumericGridDef> FromByteArray(List<byte> bytes)
    {
        DataTransferObject? dto;
        try
        {
            dto = JsonSerializer.Deserialize<DataTransferObject>(bytes.ToArray());
        }
        catch (JsonException exception)
        {
            return new Failure<NumericGridDef>($"Could not deserialize numeric grid data: {exception.Message}");
        }

        if (dto is null)
        {
            return new Failure<NumericGridDef>("Could not deserialize numeric grid data: content was empty.");
        }

        return Create(dto.ChannelName, dto.LowerLimit, dto.UpperLimit, dto.BinSize, dto.IsLeftInclusive);
    }
}
