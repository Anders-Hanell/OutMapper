using System.Collections.Immutable;
using System.Text.Json;

namespace DataStructures;

public sealed class GraphDrawData
{
    /// <summary>
    /// JSON shape of a <see cref="GraphDrawData"/>. Internal (rather than private) so that
    /// <see cref="FigureDrawData"/> can reuse it as the element type of its own graph list, instead of
    /// duplicating the same fields in a DTO of its own.
    /// </summary>
    internal sealed class DataTransferObject
    {
        public string ChannelAName { get; set; } = "";
        public string ChannelBName { get; set; } = "";
        public double[] ChannelABinEdges { get; set; } = [];
        public double[] ChannelBBinEdges { get; set; } = [];
        public string[] CellColorsRowMajor { get; set; } = [];
        public bool DrawAxisTickLabels { get; set; }
        public bool DrawAxisTitles { get; set; }
    }

    private GraphDrawData(
        string channelAName, string channelBName, ImmutableArray<double> channelABinEdges,
        ImmutableArray<double> channelBBinEdges, ImmutableArray<string> cellColorsRowMajor,
        bool drawAxisTickLabels, bool drawAxisTitles, int rowCount, int colCount)
    {
        // Private constructor to make sure object creation goes through Create().
        ChannelAName = channelAName;
        ChannelBName = channelBName;
        ChannelABinEdges = channelABinEdges;
        ChannelBBinEdges = channelBBinEdges;
        CellColorsRowMajor = cellColorsRowMajor;
        DrawAxisTickLabels = drawAxisTickLabels;
        DrawAxisTitles = drawAxisTitles;
        RowCount = rowCount;
        ColCount = colCount;
    }

    public string ChannelAName { get; }
    public string ChannelBName { get; }
    public ImmutableArray<double> ChannelABinEdges { get; }
    public ImmutableArray<double> ChannelBBinEdges { get; }
    public ImmutableArray<string> CellColorsRowMajor { get; }

    /// <summary>
    /// Whether drawing code should render numeric tick labels along the two axes. Decided once, when
    /// the graph is created (see <see cref="Create"/>), so drawing code never has to decide this itself.
    /// </summary>
    public bool DrawAxisTickLabels { get; }

    /// <summary>
    /// Whether drawing code should render the two channel names as axis titles. Decided once, when
    /// the graph is created (see <see cref="Create"/>), so drawing code never has to decide this itself.
    /// </summary>
    public bool DrawAxisTitles { get; }

    /// <summary>Number of rows in the cell grid, derived from ChannelBBinEdges.</summary>
    public int RowCount { get; }

    /// <summary>Number of columns in the cell grid, derived from ChannelABinEdges.</summary>
    public int ColCount { get; }

    public static Result<GraphDrawData> Create(
        string channelAName, string channelBName, ImmutableArray<double> channelABinEdges,
        ImmutableArray<double> channelBBinEdges, ImmutableArray<string> cellColorsRowMajor,
        bool drawAxisTickLabels, bool drawAxisTitles)
    {
        if (string.IsNullOrWhiteSpace(channelAName))
        {
            return new Failure<GraphDrawData>("Enter the first channel's name.");
        }

        if (string.IsNullOrWhiteSpace(channelBName))
        {
            return new Failure<GraphDrawData>("Enter the second channel's name.");
        }

        if (channelABinEdges.Length < 2 || !IsStrictlyIncreasing(channelABinEdges))
        {
            return new Failure<GraphDrawData>("The first channel's bin edges must have at least two, strictly increasing values.");
        }

        if (channelBBinEdges.Length < 2 || !IsStrictlyIncreasing(channelBBinEdges))
        {
            return new Failure<GraphDrawData>("The second channel's bin edges must have at least two, strictly increasing values.");
        }

        var colCount = channelABinEdges.Length - 1;
        var rowCount = channelBBinEdges.Length - 1;

        if (cellColorsRowMajor.Length != rowCount * colCount)
        {
            return new Failure<GraphDrawData>(
                $"Expected {rowCount * colCount} cell color(s) for a {rowCount} x {colCount} grid but got {cellColorsRowMajor.Length}.");
        }

        return new Success<GraphDrawData>(
            new GraphDrawData(
                channelAName, channelBName, channelABinEdges, channelBBinEdges, cellColorsRowMajor,
                drawAxisTickLabels, drawAxisTitles, rowCount, colCount));
    }

    internal DataTransferObject ToDto()
    {
        return new DataTransferObject
        {
            ChannelAName = ChannelAName,
            ChannelBName = ChannelBName,
            ChannelABinEdges = ChannelABinEdges.ToArray(),
            ChannelBBinEdges = ChannelBBinEdges.ToArray(),
            CellColorsRowMajor = CellColorsRowMajor.ToArray(),
            DrawAxisTickLabels = DrawAxisTickLabels,
            DrawAxisTitles = DrawAxisTitles
        };
    }

    internal static Result<GraphDrawData> FromDto(DataTransferObject dto)
    {
        return Create(
            dto.ChannelAName, dto.ChannelBName, dto.ChannelABinEdges.ToImmutableArray(),
            dto.ChannelBBinEdges.ToImmutableArray(), dto.CellColorsRowMajor.ToImmutableArray(),
            dto.DrawAxisTickLabels, dto.DrawAxisTitles);
    }

    public List<byte> ToByteArray()
    {
        return JsonSerializer.SerializeToUtf8Bytes(ToDto()).ToList();
    }

    public static Result<GraphDrawData> FromByteArray(List<byte> bytes)
    {
        DataTransferObject? dto;
        try
        {
            dto = JsonSerializer.Deserialize<DataTransferObject>(bytes.ToArray());
        }
        catch (JsonException exception)
        {
            return new Failure<GraphDrawData>($"Could not deserialize graph data: {exception.Message}");
        }

        if (dto is null)
        {
            return new Failure<GraphDrawData>("Could not deserialize graph data: content was empty.");
        }

        return FromDto(dto);
    }

    private static bool IsStrictlyIncreasing(ImmutableArray<double> values)
    {
        for (var i = 1; i < values.Length; i++)
        {
            if (!(values[i] > values[i - 1]))
            {
                return false;
            }
        }

        return true;
    }
}
