using System.Collections.Immutable;
using System.Text.Json;

namespace DataStructures;

/// <summary>
/// One cell of a <see cref="FigureDrawData"/> grid: either an assigned, successfully-built graph, an
/// assigned analysis whose graph could not be produced, or an unassigned/empty cell.
/// </summary>
public sealed class FigureCellDrawData
{
    private sealed class DataTransferObject
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public string? AnalysisName { get; set; }
        public string? ErrorMessage { get; set; }
        public bool HasGraph { get; set; }
        public string GraphChannelAName { get; set; } = "";
        public string GraphChannelBName { get; set; } = "";
        public double[] GraphChannelABinEdges { get; set; } = [];
        public double[] GraphChannelBBinEdges { get; set; } = [];
        public string[] GraphCellColorsRowMajor { get; set; } = [];
        public bool GraphDrawAxisTickLabels { get; set; }
        public bool GraphDrawAxisTitles { get; set; }
    }

    private FigureCellDrawData(int row, int col, string? analysisName, GraphDrawData? graph, string? errorMessage)
    {
        // Private constructor to make sure object creation goes through Create().
        Row = row;
        Col = col;
        AnalysisName = analysisName;
        Graph = graph;
        ErrorMessage = errorMessage;
    }

    public int Row { get; }
    public int Col { get; }

    /// <summary>Name of the analysis assigned to this cell, or null when the cell has no analysis assigned.</summary>
    public string? AnalysisName { get; }

    /// <summary>The cell's validated graph, or null when the cell has no graph (unassigned, or assigned but errored).</summary>
    public GraphDrawData? Graph { get; }

    /// <summary>Non-null when an analysis was assigned to this cell but its graph could not be produced.</summary>
    public string? ErrorMessage { get; }

    public bool HasGraph => Graph is not null;

    public static Result<FigureCellDrawData> Create(
        int row, int col, string? analysisName, GraphDrawData? graph, string? errorMessage)
    {
        if (row < 0)
        {
            return new Failure<FigureCellDrawData>("A figure cell's row cannot be negative.");
        }

        if (col < 0)
        {
            return new Failure<FigureCellDrawData>("A figure cell's column cannot be negative.");
        }

        if (graph is not null && errorMessage is not null)
        {
            return new Failure<FigureCellDrawData>("A figure cell cannot have both a graph and an error message.");
        }

        if (string.IsNullOrWhiteSpace(analysisName) && (graph is not null || errorMessage is not null))
        {
            return new Failure<FigureCellDrawData>("A figure cell with a graph or an error must have an analysis name.");
        }

        return new Success<FigureCellDrawData>(new FigureCellDrawData(row, col, analysisName, graph, errorMessage));
    }

    public List<byte> ToByteArray()
    {
        var dto = new DataTransferObject
        {
            Row = Row,
            Col = Col,
            AnalysisName = AnalysisName,
            ErrorMessage = ErrorMessage,
            HasGraph = Graph is not null,
            GraphChannelAName = Graph?.ChannelAName ?? "",
            GraphChannelBName = Graph?.ChannelBName ?? "",
            GraphChannelABinEdges = Graph?.ChannelABinEdges.ToArray() ?? [],
            GraphChannelBBinEdges = Graph?.ChannelBBinEdges.ToArray() ?? [],
            GraphCellColorsRowMajor = Graph?.CellColorsRowMajor.ToArray() ?? [],
            GraphDrawAxisTickLabels = Graph?.DrawAxisTickLabels ?? false,
            GraphDrawAxisTitles = Graph?.DrawAxisTitles ?? false
        };

        return JsonSerializer.SerializeToUtf8Bytes(dto).ToList();
    }

    public static Result<FigureCellDrawData> FromByteArray(List<byte> bytes)
    {
        DataTransferObject? dto;
        try
        {
            dto = JsonSerializer.Deserialize<DataTransferObject>(bytes.ToArray());
        }
        catch (JsonException exception)
        {
            return new Failure<FigureCellDrawData>($"Could not deserialize figure cell data: {exception.Message}");
        }

        if (dto is null)
        {
            return new Failure<FigureCellDrawData>("Could not deserialize figure cell data: content was empty.");
        }

        GraphDrawData? graph = null;
        if (dto.HasGraph)
        {
            switch (GraphDrawData.Create(
                dto.GraphChannelAName, dto.GraphChannelBName, dto.GraphChannelABinEdges.ToImmutableArray(),
                dto.GraphChannelBBinEdges.ToImmutableArray(), dto.GraphCellColorsRowMajor.ToImmutableArray(),
                dto.GraphDrawAxisTickLabels, dto.GraphDrawAxisTitles))
            {
                case Success<GraphDrawData> success:
                    graph = success.Value;
                    break;
                case Failure<GraphDrawData> failure:
                    return new Failure<FigureCellDrawData>(failure.Error);
                default:
                    return new Failure<FigureCellDrawData>("Could not reconstruct the cell's graph.");
            }
        }

        return Create(dto.Row, dto.Col, dto.AnalysisName, graph, dto.ErrorMessage);
    }
}
