using System.Collections.Immutable;
using System.Text.Json;

namespace DataStructures;

/// <summary>
/// A figure: a RowCount x ColCount grid of graph cells (see <see cref="FigureCellDrawData"/>), each
/// either holding a validated <see cref="GraphDrawData"/>, an error, or nothing (unassigned).
/// </summary>
public sealed class FigureDrawData
{
    private sealed class CellDto
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

    private sealed class DataTransferObject
    {
        public int RowCount { get; set; }
        public int ColCount { get; set; }
        public List<CellDto> Cells { get; set; } = new();
    }

    private FigureDrawData(int rowCount, int colCount, ImmutableArray<FigureCellDrawData> cells)
    {
        // Private constructor to make sure object creation goes through Create().
        RowCount = rowCount;
        ColCount = colCount;
        Cells = cells;
    }

    public int RowCount { get; }
    public int ColCount { get; }
    public ImmutableArray<FigureCellDrawData> Cells { get; }

    public static Result<FigureDrawData> Create(int rowCount, int colCount, IReadOnlyList<FigureCellDrawData> cells)
    {
        if (rowCount <= 0)
        {
            return new Failure<FigureDrawData>("Rows must be greater than zero.");
        }

        if (colCount <= 0)
        {
            return new Failure<FigureDrawData>("Columns must be greater than zero.");
        }

        if (cells.Count != rowCount * colCount)
        {
            return new Failure<FigureDrawData>(
                $"Expected {rowCount * colCount} cell(s) for a {rowCount} x {colCount} grid but got {cells.Count}.");
        }

        var seenPositions = new HashSet<(int Row, int Col)>();
        foreach (var cell in cells)
        {
            if (cell.Row >= rowCount || cell.Col >= colCount)
            {
                return new Failure<FigureDrawData>(
                    $"Cell at row {cell.Row}, column {cell.Col} is outside the {rowCount} x {colCount} grid.");
            }

            if (!seenPositions.Add((cell.Row, cell.Col)))
            {
                return new Failure<FigureDrawData>($"Cell at row {cell.Row}, column {cell.Col} is defined more than once.");
            }
        }

        return new Success<FigureDrawData>(new FigureDrawData(rowCount, colCount, cells.ToImmutableArray()));
    }

    public List<byte> ToByteArray()
    {
        var dto = new DataTransferObject
        {
            RowCount = RowCount,
            ColCount = ColCount,
            Cells = Cells.Select(cell => new CellDto
            {
                Row = cell.Row,
                Col = cell.Col,
                AnalysisName = cell.AnalysisName,
                ErrorMessage = cell.ErrorMessage,
                HasGraph = cell.Graph is not null,
                GraphChannelAName = cell.Graph?.ChannelAName ?? "",
                GraphChannelBName = cell.Graph?.ChannelBName ?? "",
                GraphChannelABinEdges = cell.Graph?.ChannelABinEdges.ToArray() ?? [],
                GraphChannelBBinEdges = cell.Graph?.ChannelBBinEdges.ToArray() ?? [],
                GraphCellColorsRowMajor = cell.Graph?.CellColorsRowMajor.ToArray() ?? [],
                GraphDrawAxisTickLabels = cell.Graph?.DrawAxisTickLabels ?? false,
                GraphDrawAxisTitles = cell.Graph?.DrawAxisTitles ?? false
            }).ToList()
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

        var cells = new List<FigureCellDrawData>(dto.Cells.Count);
        foreach (var cellDto in dto.Cells)
        {
            GraphDrawData? graph = null;
            if (cellDto.HasGraph)
            {
                switch (GraphDrawData.Create(
                    cellDto.GraphChannelAName, cellDto.GraphChannelBName, cellDto.GraphChannelABinEdges.ToImmutableArray(),
                    cellDto.GraphChannelBBinEdges.ToImmutableArray(), cellDto.GraphCellColorsRowMajor.ToImmutableArray(),
                    cellDto.GraphDrawAxisTickLabels, cellDto.GraphDrawAxisTitles))
                {
                    case Success<GraphDrawData> success:
                        graph = success.Value;
                        break;
                    case Failure<GraphDrawData> failure:
                        return new Failure<FigureDrawData>(failure.Error);
                    default:
                        return new Failure<FigureDrawData>("Could not reconstruct a cell's graph.");
                }
            }

            switch (FigureCellDrawData.Create(cellDto.Row, cellDto.Col, cellDto.AnalysisName, graph, cellDto.ErrorMessage))
            {
                case Success<FigureCellDrawData> success:
                    cells.Add(success.Value);
                    break;
                case Failure<FigureCellDrawData> failure:
                    return new Failure<FigureDrawData>(failure.Error);
                default:
                    return new Failure<FigureDrawData>("Could not reconstruct a figure cell.");
            }
        }

        return Create(dto.RowCount, dto.ColCount, cells);
    }
}
