namespace DataStructures;

public sealed class TwoVariableAnalysisSettings
{
    private TwoVariableAnalysisSettings(
        string cohortName,
        string channelAName, double channelARangeStart, double channelARangeEnd, double channelABinWidth,
        string channelBName, double channelBRangeStart, double channelBRangeEnd, double channelBBinWidth,
        string? rangeWarning)
    {
        // Private constructor to make sure object creation goes through Create().
        CohortName = cohortName;
        ChannelAName = channelAName;
        ChannelARangeStart = channelARangeStart;
        ChannelARangeEnd = channelARangeEnd;
        ChannelABinWidth = channelABinWidth;
        ChannelBName = channelBName;
        ChannelBRangeStart = channelBRangeStart;
        ChannelBRangeEnd = channelBRangeEnd;
        ChannelBBinWidth = channelBBinWidth;
        RangeWarning = rangeWarning;
    }

    public string CohortName { get; }
    public string ChannelAName { get; }
    public double ChannelARangeStart { get; }
    public double ChannelARangeEnd { get; }
    public double ChannelABinWidth { get; }
    public string ChannelBName { get; }
    public double ChannelBRangeStart { get; }
    public double ChannelBRangeEnd { get; }
    public double ChannelBBinWidth { get; }

    /// <summary>
    /// Non-null when a channel's range is not made up of a whole number of bins at its bin width;
    /// that channel's last bin will be narrower than the others. Does not block generation.
    /// </summary>
    public string? RangeWarning { get; }

    public static Result<TwoVariableAnalysisSettings> Create(
        string cohortName,
        string channelAName, double channelARangeStart, double channelARangeEnd, double channelABinWidth,
        string channelBName, double channelBRangeStart, double channelBRangeEnd, double channelBBinWidth)
    {
        if (string.IsNullOrWhiteSpace(cohortName))
        {
            return new Failure<TwoVariableAnalysisSettings>("Select a cohort.");
        }

        if (string.IsNullOrWhiteSpace(channelAName))
        {
            return new Failure<TwoVariableAnalysisSettings>("Enter a name for the first channel.");
        }

        if (string.IsNullOrWhiteSpace(channelBName))
        {
            return new Failure<TwoVariableAnalysisSettings>("Enter a name for the second channel.");
        }

        if (channelAName == channelBName)
        {
            return new Failure<TwoVariableAnalysisSettings>("The two channels must be different.");
        }

        if (!double.IsFinite(channelARangeStart) || !double.IsFinite(channelARangeEnd) ||
            channelARangeEnd <= channelARangeStart)
        {
            return new Failure<TwoVariableAnalysisSettings>(
                "The end of the range for the first channel must be greater than the start.");
        }

        if (!double.IsFinite(channelBRangeStart) || !double.IsFinite(channelBRangeEnd) ||
            channelBRangeEnd <= channelBRangeStart)
        {
            return new Failure<TwoVariableAnalysisSettings>(
                "The end of the range for the second channel must be greater than the start.");
        }

        if (!double.IsFinite(channelABinWidth) || channelABinWidth <= 0)
        {
            return new Failure<TwoVariableAnalysisSettings>("The bin width for the first channel must be a positive number.");
        }

        if (!double.IsFinite(channelBBinWidth) || channelBBinWidth <= 0)
        {
            return new Failure<TwoVariableAnalysisSettings>("The bin width for the second channel must be a positive number.");
        }

        var warnings = new List<string>();
        if (!IsWholeNumberOfBins(channelARangeStart, channelARangeEnd, channelABinWidth))
        {
            warnings.Add("The first channel's range is not made up of a whole number of bins; its last bin will be narrower than the others.");
        }

        if (!IsWholeNumberOfBins(channelBRangeStart, channelBRangeEnd, channelBBinWidth))
        {
            warnings.Add("The second channel's range is not made up of a whole number of bins; its last bin will be narrower than the others.");
        }

        return new Success<TwoVariableAnalysisSettings>(
            new TwoVariableAnalysisSettings(
                cohortName,
                channelAName, channelARangeStart, channelARangeEnd, channelABinWidth,
                channelBName, channelBRangeStart, channelBRangeEnd, channelBBinWidth,
                warnings.Count == 0 ? null : string.Join(" ", warnings)));
    }

    private static bool IsWholeNumberOfBins(double rangeStart, double rangeEnd, double binWidth)
    {
        var binCount = (rangeEnd - rangeStart) / binWidth;
        return Math.Abs(binCount - Math.Round(binCount)) < 1e-9;
    }
}
