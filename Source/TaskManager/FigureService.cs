using System.Collections.Immutable;
using System.IO;
using System.Text.Json;

using Messages;

namespace TaskManager;

internal static class FigureService
{
    private const string FigureConfigFileName = "figure-config.json";

    internal static FigureLayoutResponse ReadLayout(string? workspaceFolder, string projectName, string figureName)
    {
        if (string.IsNullOrWhiteSpace(workspaceFolder))
        {
            return NoLayout(projectName, figureName);
        }

        var config = ReadConfig(workspaceFolder, projectName, figureName);
        if (config is null)
        {
            return NoLayout(projectName, figureName);
        }

        return new FigureLayoutResponse(
            projectName, figureName, LayoutExists: true, config.RowCount, config.ColCount,
            config.CellAnalysisNames.ToImmutableArray());
    }

    internal static SaveFigureSizeResponse SaveSize(
        string? workspaceFolder, string projectName, string figureName, int rowCount, int colCount)
    {
        if (string.IsNullOrWhiteSpace(workspaceFolder) || string.IsNullOrWhiteSpace(projectName) ||
            string.IsNullOrWhiteSpace(figureName))
        {
            return new SaveFigureSizeResponse(
                projectName, figureName, Success: false, "No workspace, project, or figure was specified.",
                0, 0, ImmutableArray<string?>.Empty);
        }

        if (rowCount <= 0 || colCount <= 0)
        {
            return new SaveFigureSizeResponse(
                projectName, figureName, Success: false, "Rows and columns must both be greater than zero.",
                0, 0, ImmutableArray<string?>.Empty);
        }

        var figureFolder = Path.Combine(
            workspaceFolder, "Projects", projectName, "OutMapper_InternalFiles", "Figures", figureName);
        if (!Directory.Exists(figureFolder))
        {
            return new SaveFigureSizeResponse(
                projectName, figureName, Success: false, $"Figure '{figureName}' does not exist.",
                0, 0, ImmutableArray<string?>.Empty);
        }

        var existingConfig = ReadConfig(workspaceFolder, projectName, figureName);
        var remappedCells = new string?[rowCount * colCount];

        if (existingConfig is not null)
        {
            for (var row = 0; row < rowCount && row < existingConfig.RowCount; row++)
            {
                for (var col = 0; col < colCount && col < existingConfig.ColCount; col++)
                {
                    remappedCells[row * colCount + col] =
                        existingConfig.CellAnalysisNames[row * existingConfig.ColCount + col];
                }
            }
        }

        var newConfig = new FigureConfigDto
        {
            RowCount = rowCount,
            ColCount = colCount,
            CellAnalysisNames = remappedCells
        };

        if (!WriteConfig(workspaceFolder, projectName, figureName, newConfig))
        {
            return new SaveFigureSizeResponse(
                projectName, figureName, Success: false, "Could not save the figure's size.",
                0, 0, ImmutableArray<string?>.Empty);
        }

        return new SaveFigureSizeResponse(
            projectName, figureName, Success: true, ErrorMessage: null, rowCount, colCount,
            remappedCells.ToImmutableArray());
    }

    internal static CreateFigureGraphResponse CreateGraph(
        string? workspaceFolder, string projectName, string figureName, int rowCount, int colCount,
        ImmutableArray<string?> cellAnalysisNames)
    {
        if (string.IsNullOrWhiteSpace(workspaceFolder) || string.IsNullOrWhiteSpace(projectName) ||
            string.IsNullOrWhiteSpace(figureName))
        {
            return new CreateFigureGraphResponse(
                projectName, figureName, Success: false, "No workspace, project, or figure was specified.",
                0, 0, ImmutableArray<FigureCellGraphData>.Empty);
        }

        if (rowCount <= 0 || colCount <= 0 || cellAnalysisNames.Length != rowCount * colCount)
        {
            return new CreateFigureGraphResponse(
                projectName, figureName, Success: false, "The figure's grid dimensions are invalid.",
                0, 0, ImmutableArray<FigureCellGraphData>.Empty);
        }

        var config = new FigureConfigDto
        {
            RowCount = rowCount,
            ColCount = colCount,
            CellAnalysisNames = cellAnalysisNames.ToArray()
        };
        WriteConfig(workspaceFolder, projectName, figureName, config);

        var cells = new List<FigureCellGraphData>(rowCount * colCount);

        for (var row = 0; row < rowCount; row++)
        {
            for (var col = 0; col < colCount; col++)
            {
                var analysisName = cellAnalysisNames[row * colCount + col];

                if (string.IsNullOrWhiteSpace(analysisName))
                {
                    cells.Add(new FigureCellGraphData(
                        row, col, AnalysisName: null, HasGraph: false, ErrorMessage: null,
                        ChannelAName: null, ChannelBName: null,
                        ImmutableArray<double>.Empty, ImmutableArray<double>.Empty, ImmutableArray<string>.Empty));
                    continue;
                }

                var graphData = AnalysisService.ReadPersistedGraphData(workspaceFolder, projectName, analysisName);
                if (!graphData.Found)
                {
                    cells.Add(new FigureCellGraphData(
                        row, col, analysisName, HasGraph: false,
                        $"No persisted graph data for analysis '{analysisName}'.",
                        ChannelAName: null, ChannelBName: null,
                        ImmutableArray<double>.Empty, ImmutableArray<double>.Empty, ImmutableArray<string>.Empty));
                    continue;
                }

                cells.Add(new FigureCellGraphData(
                    row, col, analysisName, HasGraph: true, ErrorMessage: null,
                    graphData.ChannelAName, graphData.ChannelBName,
                    graphData.ChannelABinEdges, graphData.ChannelBBinEdges, graphData.CellColorsRowMajor));
            }
        }

        return new CreateFigureGraphResponse(
            projectName, figureName, Success: true, ErrorMessage: null, rowCount, colCount, cells.ToImmutableArray());
    }

    private static FigureLayoutResponse NoLayout(string projectName, string figureName)
    {
        return new FigureLayoutResponse(projectName, figureName, LayoutExists: false, 0, 0, ImmutableArray<string?>.Empty);
    }

    private static FigureConfigDto? ReadConfig(string workspaceFolder, string projectName, string figureName)
    {
        var configFilePath = ResolveConfigFilePath(workspaceFolder, projectName, figureName);
        if (!File.Exists(configFilePath))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<FigureConfigDto>(File.ReadAllBytes(configFilePath));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool WriteConfig(string workspaceFolder, string projectName, string figureName, FigureConfigDto config)
    {
        try
        {
            var configFilePath = ResolveConfigFilePath(workspaceFolder, projectName, figureName);
            Directory.CreateDirectory(Path.GetDirectoryName(configFilePath)!);
            File.WriteAllBytes(configFilePath, JsonSerializer.SerializeToUtf8Bytes(config));
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static string ResolveConfigFilePath(string workspaceFolder, string projectName, string figureName)
    {
        return Path.Combine(
            workspaceFolder, "Projects", projectName, "OutMapper_InternalFiles", "Figures", figureName, FigureConfigFileName);
    }

    private sealed class FigureConfigDto
    {
        public int RowCount { get; set; }
        public int ColCount { get; set; }
        public string?[] CellAnalysisNames { get; set; } = [];
    }
}
