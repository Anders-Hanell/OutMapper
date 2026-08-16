namespace DataStructures;

public sealed class TwoVariableAnalysisSettings
{
    private TwoVariableAnalysisSettings(
        string cohortName, string channelAName, double channelABinSize, string channelBName, double channelBBinSize)
    {
        // Private constructor to make sure object creation goes through Create().
        CohortName = cohortName;
        ChannelAName = channelAName;
        ChannelABinSize = channelABinSize;
        ChannelBName = channelBName;
        ChannelBBinSize = channelBBinSize;
    }

    public string CohortName { get; }
    public string ChannelAName { get; }
    public double ChannelABinSize { get; }
    public string ChannelBName { get; }
    public double ChannelBBinSize { get; }

    public static Result<TwoVariableAnalysisSettings> Create(
        string cohortName, string channelAName, double channelABinSize, string channelBName, double channelBBinSize)
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

        if (!double.IsFinite(channelABinSize) || channelABinSize <= 0)
        {
            return new Failure<TwoVariableAnalysisSettings>("The bin size for the first channel must be a positive number.");
        }

        if (!double.IsFinite(channelBBinSize) || channelBBinSize <= 0)
        {
            return new Failure<TwoVariableAnalysisSettings>("The bin size for the second channel must be a positive number.");
        }

        return new Success<TwoVariableAnalysisSettings>(
            new TwoVariableAnalysisSettings(cohortName, channelAName, channelABinSize, channelBName, channelBBinSize));
    }
}
