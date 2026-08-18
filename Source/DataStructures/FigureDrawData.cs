using System.Collections.Immutable;
using System.Text.Json;

namespace DataStructures;

/// <summary>
/// A figure: a RowCount x ColCount grid of cells, each either holding a validated
/// <see cref="GraphDrawData"/> or nothing (unassigned). <see cref="CellHasGraph"/> is a row-major mask
/// (the cell at row R, column C is at index R * ColCount + C) saying which grid positions have a graph;
/// <see cref="Graphs"/> holds just those graphs, in the same row-major order as their "true" entries in
/// <see cref="CellHasGraph"/>.
/// </summary>
public sealed class FigureDrawData
{
    private sealed class DataTransferObject
    {
        public int RowCount { get; set; }
        public int ColCount { get; set; }
        public bool[] CellHasGraph { get; set; } = [];
        public List<GraphDrawData.DataTransferObject> Graphs { get; set; } = new();
    }

    private FigureDrawData(int rowCount, int colCount, ImmutableArray<bool> cellHasGraph, ImmutableArray<GraphDrawData> graphs)
    {
        // Private constructor to make sure object creation goes through Create().
        RowCount = rowCount;
        ColCount = colCount;
        CellHasGraph = cellHasGraph;
        Graphs = graphs;
    }

    public int RowCount { get; }
    public int ColCount { get; }
    public ImmutableArray<bool> CellHasGraph { get; }
    public ImmutableArray<GraphDrawData> Graphs { get; }

    public static Result<FigureDrawData> Create(
        int rowCount, int colCount, IReadOnlyList<bool> cellHasGraph, IReadOnlyList<GraphDrawData> graphs)
    {
        if (rowCount <= 0)
        {
            return new Failure<FigureDrawData>("Rows must be greater than zero.");
        }

        if (colCount <= 0)
        {
            return new Failure<FigureDrawData>("Columns must be greater than zero.");
        }

        if (cellHasGraph.Count != rowCount * colCount)
        {
            return new Failure<FigureDrawData>(
                $"Expected {rowCount * colCount} cell flag(s) for a {rowCount} x {colCount} grid but got {cellHasGraph.Count}.");
        }

        var expectedGraphCount = cellHasGraph.Count(hasGraph => hasGraph);
        if (graphs.Count != expectedGraphCount)
        {
            return new Failure<FigureDrawData>(
                $"Expected {expectedGraphCount} assigned graph(s) but got {graphs.Count}.");
        }

        return new Success<FigureDrawData>(
            new FigureDrawData(rowCount, colCount, cellHasGraph.ToImmutableArray(), graphs.ToImmutableArray()));
    }

    public List<byte> ToByteArray()
    {
        var dto = new DataTransferObject
        {
            RowCount = RowCount,
            ColCount = ColCount,
            CellHasGraph = CellHasGraph.ToArray(),
            Graphs = Graphs.Select(graph => graph.ToDto()).ToList()
        };

        return JsonSerializer.SerializeToUtf8Bytes(dto).ToList();
    }

    public static Result<FigureDrawData> FromByteArray(List<byte> bytes)
    {
        DataTransferObject? dto;
        try
        {
            dto = JsonSerializer.Deserialize<DataTransferObject>(bytes.ToArray());
        }
        catch (JsonException exception)
        {
            return new Failure<FigureDrawData>($"Could not deserialize figure data: {exception.Message}");
        }

        if (dto is null)
        {
            return new Failure<FigureDrawData>("Could not deserialize figure data: content was empty.");
        }

        var graphs = new List<GraphDrawData>(dto.Graphs.Count);
        foreach (var graphDto in dto.Graphs)
        {
            switch (GraphDrawData.FromDto(graphDto))
            {
                case Success<GraphDrawData> success:
                    graphs.Add(success.Value);
                    break;
                case Failure<GraphDrawData> failure:
                    return new Failure<FigureDrawData>(failure.Error);
                default:
                    return new Failure<FigureDrawData>("Could not reconstruct a figure's graph.");
            }
        }

        return Create(dto.RowCount, dto.ColCount, dto.CellHasGraph, graphs);
    }
}
