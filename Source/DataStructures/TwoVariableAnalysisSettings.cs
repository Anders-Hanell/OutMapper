using System.Text.Json;

namespace DataStructures;

public sealed class TwoVariableAnalysisSettings
{
    private sealed class DataTransferObject
    {
        public string CohortName { get; set; } = "";
        public string ChannelAName { get; set; } = "";
        public double ChannelALowerLimit { get; set; }
        public double ChannelAUpperLimit { get; set; }
        public double ChannelABinSize { get; set; }
        public bool ChannelAIsLeftInclusive { get; set; } = true;
        public string ChannelBName { get; set; } = "";
        public double ChannelBLowerLimit { get; set; }
        public double ChannelBUpperLimit { get; set; }
        public double ChannelBBinSize { get; set; }
        public bool ChannelBIsLeftInclusive { get; set; } = true;
    }

    private TwoVariableAnalysisSettings(
        string cohortName, NumericGridDef channelAGrid, NumericGridDef channelBGrid, string? rangeWarning)
    {
        // Private constructor to make sure object creation goes through Create().
        CohortName = cohortName;
        ChannelAGrid = channelAGrid;
        ChannelBGrid = channelBGrid;
        RangeWarning = rangeWarning;
    }

    public string CohortName { get; }
    public NumericGridDef ChannelAGrid { get; }
    public NumericGridDef ChannelBGrid { get; }

    /// <summary>
    /// Non-null when a channel's range is not made up of a whole number of bins at its bin size;
    /// that channel's last bin will be narrower than the others. Does not block generation.
    /// </summary>
    public string? RangeWarning { get; }

    public static Result<TwoVariableAnalysisSettings> Create(
        string cohortName, NumericGridDef channelAGrid, NumericGridDef channelBGrid)
    {
        if (string.IsNullOrWhiteSpace(cohortName))
        {
            return new Failure<TwoVariableAnalysisSettings>("Select a cohort.");
        }

        if (channelAGrid.ChannelName == channelBGrid.ChannelName)
        {
            return new Failure<TwoVariableAnalysisSettings>("The two channels must be different.");
        }

        var warnings = new List<string>();
        if (channelAGrid.HasPartialLastBin)
        {
            warnings.Add("The first channel's range is not made up of a whole number of bins; its last bin will be narrower than the others.");
        }

        if (channelBGrid.HasPartialLastBin)
        {
            warnings.Add("The second channel's range is not made up of a whole number of bins; its last bin will be narrower than the others.");
        }

        return new Success<TwoVariableAnalysisSettings>(
            new TwoVariableAnalysisSettings(
                cohortName, channelAGrid, channelBGrid, warnings.Count == 0 ? null : string.Join(" ", warnings)));
    }

    public List<byte> ToByteArray()
    {
        var dto = new DataTransferObject
        {
            CohortName = CohortName,
            ChannelAName = ChannelAGrid.ChannelName,
            ChannelALowerLimit = ChannelAGrid.LowerLimit,
            ChannelAUpperLimit = ChannelAGrid.UpperLimit,
            ChannelABinSize = ChannelAGrid.BinSize,
            ChannelAIsLeftInclusive = ChannelAGrid.IsLeftInclusive,
            ChannelBName = ChannelBGrid.ChannelName,
            ChannelBLowerLimit = ChannelBGrid.LowerLimit,
            ChannelBUpperLimit = ChannelBGrid.UpperLimit,
            ChannelBBinSize = ChannelBGrid.BinSize,
            ChannelBIsLeftInclusive = ChannelBGrid.IsLeftInclusive
        };

        return JsonSerializer.SerializeToUtf8Bytes(dto).ToList();
    }

    public static Result<TwoVariableAnalysisSettings> FromByteArray(List<byte> bytes)
    {
        DataTransferObject? dto;
        try
        {
            dto = JsonSerializer.Deserialize<DataTransferObject>(bytes.ToArray());
        }
        catch (JsonException exception)
        {
            return new Failure<TwoVariableAnalysisSettings>($"Could not deserialize analysis settings data: {exception.Message}");
        }

        if (dto is null)
        {
            return new Failure<TwoVariableAnalysisSettings>("Could not deserialize analysis settings data: content was empty.");
        }

        NumericGridDef channelAGrid;
        switch (NumericGridDef.Create(
            dto.ChannelAName, dto.ChannelALowerLimit, dto.ChannelAUpperLimit, dto.ChannelABinSize, dto.ChannelAIsLeftInclusive))
        {
            case Success<NumericGridDef> success:
                channelAGrid = success.Value;
                break;
            case Failure<NumericGridDef> failure:
                return new Failure<TwoVariableAnalysisSettings>(failure.Error);
            default:
                return new Failure<TwoVariableAnalysisSettings>("Could not reconstruct the first channel's grid definition.");
        }

        NumericGridDef channelBGrid;
        switch (NumericGridDef.Create(
            dto.ChannelBName, dto.ChannelBLowerLimit, dto.ChannelBUpperLimit, dto.ChannelBBinSize, dto.ChannelBIsLeftInclusive))
        {
            case Success<NumericGridDef> success:
                channelBGrid = success.Value;
                break;
            case Failure<NumericGridDef> failure:
                return new Failure<TwoVariableAnalysisSettings>(failure.Error);
            default:
                return new Failure<TwoVariableAnalysisSettings>("Could not reconstruct the second channel's grid definition.");
        }

        return Create(dto.CohortName, channelAGrid, channelBGrid);
    }
}
