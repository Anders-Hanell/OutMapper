using System.Collections.Immutable;
using System.IO;
using System.Text.Json;

using DataStructures;
using Messages;

namespace TaskManager;

internal static class FigureService
{
    private const string FigureConfigFileName = "figure-config.json";

    internal static FigureLayoutResponse ReadLayout(
        IFileSystem fileSystem, string? projectFolder, string figureName)
    {
        if (string.IsNullOrWhiteSpace(projectFolder))
        {
            return NoLayout(projectFolder ?? string.Empty, figureName);
        }

        var config = ReadConfig(fileSystem, projectFolder, figureName);
        if (config is null)
        {
            return NoLayout(projectFolder, figureName);
        }

        return new FigureLayoutResponse(
            projectFolder, figureName, LayoutExists: true, config.RowCount, config.ColCount,
            config.CellAnalysisNames.ToImmutableArray());
    }

    internal static SaveFigureSizeResponse SaveSize(
        IFileSystem fileSystem, string? projectFolder, string figureName, int rowCount, int colCount)
    {
        if (string.IsNullOrWhiteSpace(projectFolder) || string.IsNullOrWhiteSpace(figureName))
        {
            return new SaveFigureSizeResponse(
                projectFolder ?? string.Empty, figureName, Success: false, "No project or figure was specified.",
                0, 0, ImmutableArray<string?>.Empty);
        }

        if (rowCount <= 0 || colCount <= 0)
        {
            return new SaveFigureSizeResponse(
                projectFolder, figureName, Success: false, "Rows and columns must both be greater than zero.",
                0, 0, ImmutableArray<string?>.Empty);
        }

        var figureFolder = Path.Combine(projectFolder, "OutMapper_InternalFiles", "Figures", figureName);
        if (!fileSystem.DirectoryExists(figureFolder))
        {
            return new SaveFigureSizeResponse(
                projectFolder, figureName, Success: false, $"Figure '{figureName}' does not exist.",
                0, 0, ImmutableArray<string?>.Empty);
        }

        var existingConfig = ReadConfig(fileSystem, projectFolder, figureName);
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

        if (!WriteConfig(fileSystem, projectFolder, figureName, newConfig))
        {
            return new SaveFigureSizeResponse(
                projectFolder, figureName, Success: false, "Could not save the figure's size.",
                0, 0, ImmutableArray<string?>.Empty);
        }

        return new SaveFigureSizeResponse(
            projectFolder, figureName, Success: true, ErrorMessage: null, rowCount, colCount,
            remappedCells.ToImmutableArray());
    }

    internal static CreateFigureGraphResponse CreateGraph(
        IFileSystem fileSystem, string? projectFolder, string figureName, int rowCount, int colCount,
        ImmutableArray<string?> cellAnalysisNames)
    {
        if (string.IsNullOrWhiteSpace(projectFolder) || string.IsNullOrWhiteSpace(figureName))
        {
            return new CreateFigureGraphResponse(
                projectFolder ?? string.Empty, figureName, Success: false, "No project or figure was specified.", Figure: null);
        }

        if (rowCount <= 0 || colCount <= 0 || cellAnalysisNames.Length != rowCount * colCount)
        {
            return new CreateFigureGraphResponse(
                projectFolder, figureName, Success: false, "The figure's grid dimensions are invalid.", Figure: null);
        }

        var config = new FigureConfigDto
        {
            RowCount = rowCount,
            ColCount = colCount,
            CellAnalysisNames = cellAnalysisNames.ToArray()
        };
        WriteConfig(fileSystem, projectFolder, figureName, config);

        var cells = new List<FigureCellDrawData>(rowCount * colCount);

        for (var row = 0; row < rowCount; row++)
        {
            for (var col = 0; col < colCount; col++)
            {
                var analysisName = cellAnalysisNames[row * colCount + col];

                Result<FigureCellDrawData> cellResult;
                if (string.IsNullOrWhiteSpace(analysisName))
                {
                    cellResult = FigureCellDrawData.Create(row, col, analysisName: null, graph: null, errorMessage: null);
                }
                else
                {
                    var graph = AnalysisService.ReadPersistedGraphData(fileSystem, projectFolder, analysisName);
                    cellResult = graph is null
                        ? FigureCellDrawData.Create(
                            row, col, analysisName, graph: null, $"No persisted graph data for analysis '{analysisName}'.")
                        : FigureCellDrawData.Create(row, col, analysisName, graph, errorMessage: null);
                }

                switch (cellResult)
                {
                    case Success<FigureCellDrawData> success:
                        cells.Add(success.Value);
                        break;
                    case Failure<FigureCellDrawData> failure:
                        return new CreateFigureGraphResponse(projectFolder, figureName, Success: false, failure.Error, Figure: null);
                }
            }
        }

        switch (FigureDrawData.Create(rowCount, colCount, cells))
        {
            case Success<FigureDrawData> success:
                return new CreateFigureGraphResponse(projectFolder, figureName, Success: true, ErrorMessage: null, success.Value);
            case Failure<FigureDrawData> failure:
                return new CreateFigureGraphResponse(projectFolder, figureName, Success: false, failure.Error, Figure: null);
            default:
                return new CreateFigureGraphResponse(
                    projectFolder, figureName, Success: false, "Could not build the figure's draw data.", Figure: null);
        }
    }

    private static FigureLayoutResponse NoLayout(string projectFolder, string figureName)
    {
        return new FigureLayoutResponse(projectFolder, figureName, LayoutExists: false, 0, 0, ImmutableArray<string?>.Empty);
    }

    private static FigureConfigDto? ReadConfig(IFileSystem fileSystem, string projectFolder, string figureName)
    {
        var configFilePath = ResolveConfigFilePath(projectFolder, figureName);
        if (!fileSystem.FileExists(configFilePath))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<FigureConfigDto>(fileSystem.ReadAllBytes(configFilePath));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool WriteConfig(
        IFileSystem fileSystem, string projectFolder, string figureName, FigureConfigDto config)
    {
        try
        {
            var configFilePath = ResolveConfigFilePath(projectFolder, figureName);
            fileSystem.CreateDirectory(Path.GetDirectoryName(configFilePath)!);
            fileSystem.WriteAllBytes(configFilePath, JsonSerializer.SerializeToUtf8Bytes(config));
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static string ResolveConfigFilePath(string projectFolder, string figureName)
    {
        return Path.Combine(projectFolder, "OutMapper_InternalFiles", "Figures", figureName, FigureConfigFileName);
    }

    private sealed class FigureConfigDto
    {
        public int RowCount { get; set; }
        public int ColCount { get; set; }
        public string?[] CellAnalysisNames { get; set; } = [];
    }
}
