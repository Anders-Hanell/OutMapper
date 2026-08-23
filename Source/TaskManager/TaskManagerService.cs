using System.Collections.Immutable;
using System.IO;
using System.Text.Json;
using System.Threading.Channels;
using Messages;

namespace TaskManager;

internal static class TaskManagerService
{
    private static readonly Channel<Message> _messageChannel = Channel.CreateUnbounded<Message>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    private static readonly CancellationTokenSource _cts = new();
    private static Task? _processingTask;

    public static void Start()
    {
        if (_processingTask is not null)
        {
            return;
        }

        _processingTask = Task.Run(() => ProcessMessagesAsync(_cts.Token));
    }

    public static bool EnqueueMessage(Message message)
    {
        Start();
        return _messageChannel.Writer.TryWrite(message);
    }

    public static async ValueTask<bool> EnqueueMessageAsync(Message message)
    {
        Start();
        await _messageChannel.Writer.WriteAsync(message);
        return true;
    }

    private static async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        await foreach (var message in _messageChannel.Reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                await MessageRouter.Route(message);
            }
            catch (Exception)
            {
                // A single message must never be able to bring down the processing loop -
                // doing so would silently stop all future requests from ever getting a response.
            }
        }
    }

    internal static Task HandleDatasetListRequestAsync(DatasetListRequest message)
    {
        var datasetNames = LocateDatasets(LocalFileSystem.Instance, message.ProjectFolder);

        var response = new DatasetListResponse(message.ProjectFolder, datasetNames);

        GatewayToOutMapper.SendMessage(response);
        return Task.CompletedTask;
    }

    internal static Task HandleCreateDatasetRequestAsync(CreateDatasetRequest message)
    {
        var createdDataset = CreateDataset(
            LocalFileSystem.Instance, message.ProjectFolder, message.DatasetName, message.RawDataFolderPath);

        var response = new CreateDatasetResponse(message.DatasetName, message.ProjectFolder, createdDataset);

        GatewayToOutMapper.SendMessage(response);
        return Task.CompletedTask;
    }

    internal static async Task HandleParseDatasetRequestAsync(ParseDatasetRequest message)
    {
        var response = await DatasetParsingService.ParseDatasetAsync(
            LocalFileSystem.Instance, message.ProjectFolder, message.DatasetName, message.ParseParams);

        GatewayToOutMapper.SendMessage(response);
    }

    internal static Task HandleParseResultRequestAsync(ParseResultRequest message)
    {
        var response = DatasetParsingService.ReadPersistedParseResult(
            LocalFileSystem.Instance, message.ProjectFolder, message.DatasetName);

        GatewayToOutMapper.SendMessage(response);
        return Task.CompletedTask;
    }

    internal static Task HandleCohortListRequestAsync(CohortListRequest message)
    {
        var cohortNames = LocateCohorts(LocalFileSystem.Instance, message.ProjectFolder);

        var response = new CohortListResponse(message.ProjectFolder, cohortNames);

        GatewayToOutMapper.SendMessage(response);
        return Task.CompletedTask;
    }

    internal static Task HandleCreateCohortRequestAsync(CreateCohortRequest message)
    {
        var createdCohort = CreateCohort(
            LocalFileSystem.Instance, message.ProjectFolder, message.CohortName,
            message.RawCsvFilePath, message.LinkedDatasetNames);

        var response = new CreateCohortResponse(message.CohortName, message.ProjectFolder, createdCohort);

        GatewayToOutMapper.SendMessage(response);
        return Task.CompletedTask;
    }

    internal static async Task HandleParseCohortRequestAsync(ParseCohortRequest message)
    {
        var response = await CohortParsingService.ParseCohortAsync(
            LocalFileSystem.Instance, message.ProjectFolder, message.CohortName, message.ParseParams);

        GatewayToOutMapper.SendMessage(response);
    }

    internal static Task HandleCohortParseResultRequestAsync(CohortParseResultRequest message)
    {
        var response = CohortParsingService.ReadPersistedParseResult(
            LocalFileSystem.Instance, message.ProjectFolder, message.CohortName);

        GatewayToOutMapper.SendMessage(response);
        return Task.CompletedTask;
    }

    internal static Task HandleAnalysisListRequestAsync(AnalysisListRequest message)
    {
        var analysisNames = LocateAnalyses(LocalFileSystem.Instance, message.ProjectFolder);

        var response = new AnalysisListResponse(message.ProjectFolder, analysisNames);

        GatewayToOutMapper.SendMessage(response);
        return Task.CompletedTask;
    }

    internal static Task HandleCreateAnalysisRequestAsync(CreateAnalysisRequest message)
    {
        var createdAnalysis = CreateAnalysis(LocalFileSystem.Instance, message.ProjectFolder, message.AnalysisName);

        var response = new CreateAnalysisResponse(message.AnalysisName, message.ProjectFolder, createdAnalysis);

        GatewayToOutMapper.SendMessage(response);
        return Task.CompletedTask;
    }

    internal static async Task HandleGenerateAnalysisGraphRequestAsync(GenerateAnalysisGraphRequest message)
    {
        var response = await AnalysisService.GenerateGraphAsync(
            LocalFileSystem.Instance, message.ProjectFolder, message.AnalysisName, message.Settings);

        GatewayToOutMapper.SendMessage(response);
    }

    internal static Task HandleAnalysisResultRequestAsync(AnalysisResultRequest message)
    {
        var response = AnalysisService.ReadPersistedResult(
            LocalFileSystem.Instance, message.ProjectFolder, message.AnalysisName);

        GatewayToOutMapper.SendMessage(response);
        return Task.CompletedTask;
    }

    internal static Task HandleAnalysisSettingsRequestAsync(AnalysisSettingsRequest message)
    {
        var response = AnalysisService.ReadPersistedSettings(
            LocalFileSystem.Instance, message.ProjectFolder, message.AnalysisName);

        GatewayToOutMapper.SendMessage(response);
        return Task.CompletedTask;
    }

    internal static async Task HandleChannelListRequestAsync(ChannelListRequest message)
    {
        var channelNames = await AnalysisService.DiscoverChannelNamesAsync(
            LocalFileSystem.Instance, message.ProjectFolder, message.CohortName);

        var response = new ChannelListResponse(message.ProjectFolder, message.CohortName, channelNames);

        GatewayToOutMapper.SendMessage(response);
    }

    internal static Task HandleFigureListRequestAsync(FigureListRequest message)
    {
        var figureNames = LocateFigures(LocalFileSystem.Instance, message.ProjectFolder);

        var response = new FigureListResponse(message.ProjectFolder, figureNames);

        GatewayToOutMapper.SendMessage(response);
        return Task.CompletedTask;
    }

    internal static Task HandleCreateFigureRequestAsync(CreateFigureRequest message)
    {
        var createdFigure = CreateFigure(LocalFileSystem.Instance, message.ProjectFolder, message.FigureName);

        var response = new CreateFigureResponse(message.FigureName, message.ProjectFolder, createdFigure);

        GatewayToOutMapper.SendMessage(response);
        return Task.CompletedTask;
    }

    internal static Task HandleFigureLayoutRequestAsync(FigureLayoutRequest message)
    {
        var response = FigureService.ReadLayout(
            LocalFileSystem.Instance, message.ProjectFolder, message.FigureName);

        GatewayToOutMapper.SendMessage(response);
        return Task.CompletedTask;
    }

    internal static Task HandleSaveFigureSizeRequestAsync(SaveFigureSizeRequest message)
    {
        var response = FigureService.SaveSize(
            LocalFileSystem.Instance, message.ProjectFolder, message.FigureName,
            message.RowCount, message.ColCount);

        GatewayToOutMapper.SendMessage(response);
        return Task.CompletedTask;
    }

    internal static Task HandleAnalysesWithGraphListRequestAsync(AnalysesWithGraphListRequest message)
    {
        var analysisNames = AnalysisService.ListAnalysesWithPersistedGraph(
            LocalFileSystem.Instance, message.ProjectFolder);

        var response = new AnalysesWithGraphListResponse(message.ProjectFolder, analysisNames);

        GatewayToOutMapper.SendMessage(response);
        return Task.CompletedTask;
    }

    internal static Task HandleCreateFigureGraphRequestAsync(CreateFigureGraphRequest message)
    {
        var response = FigureService.CreateGraph(
            LocalFileSystem.Instance, message.ProjectFolder, message.FigureName,
            message.RowCount, message.ColCount, message.CellAnalysisNames, message.LabelStyle);

        GatewayToOutMapper.SendMessage(response);
        return Task.CompletedTask;
    }

    internal static Task HandleComputeHeatmapLayoutRequestAsync(ComputeHeatmapLayoutRequest message)
    {
        var result = HeatmapLayoutService.ComputeLayout(
            message.Data, message.AreaLeft, message.AreaTop, message.AreaWidth, message.AreaHeight);

        GatewayToOutMapper.SendMessage(new ComputeHeatmapLayoutResponse(message.RequestId, result));
        return Task.CompletedTask;
    }

    internal static Task HandleComputeFigureLayoutRequestAsync(ComputeFigureLayoutRequest message)
    {
        var result = FigureLayoutService.ComputeLayout(message.Figure);

        GatewayToOutMapper.SendMessage(new ComputeFigureLayoutResponse(message.RequestId, result));
        return Task.CompletedTask;
    }

    internal static Task HandleBuildFigureDrawDataRequestAsync(BuildFigureDrawDataRequest message)
    {
        var result = FigureService.BuildDrawData(
            LocalFileSystem.Instance, message.ProjectFolder, message.RowCount, message.ColCount,
            message.CellAnalysisNames, message.LabelStyle);

        GatewayToOutMapper.SendMessage(new BuildFigureDrawDataResponse(message.RequestId, result));
        return Task.CompletedTask;
    }

    internal static bool CreateDataset(
        IFileSystem fileSystem, string? projectFolder, string datasetName, string? rawDataFolderPath)
    {
        if (string.IsNullOrWhiteSpace(projectFolder) || string.IsNullOrWhiteSpace(datasetName))
        {
            return false;
        }

        if (!fileSystem.DirectoryExists(projectFolder))
        {
            return false;
        }

        try
        {
            var datasetsFolder = Path.Combine(projectFolder, "OutMapper_InternalFiles", "Datasets");
            fileSystem.CreateDirectory(datasetsFolder);

            var datasetFile = Path.Combine(datasetsFolder, datasetName + ".omds");
            if (fileSystem.FileExists(datasetFile))
            {
                return false;
            }

            fileSystem.CreateEmptyFile(datasetFile);

            var datasetFolder = Path.Combine(datasetsFolder, datasetName);
            var importedRawDataFolder = Path.Combine(datasetFolder, "Imported raw data");
            fileSystem.CreateDirectory(importedRawDataFolder);

            if (!string.IsNullOrWhiteSpace(rawDataFolderPath) && fileSystem.DirectoryExists(rawDataFolderPath))
            {
                foreach (var csvFile in fileSystem.GetFiles(rawDataFolderPath, "*.csv"))
                {
                    var destinationFile = Path.Combine(importedRawDataFolder, Path.GetFileName(csvFile));
                    fileSystem.CopyFile(csvFile, destinationFile, overwrite: false);
                }
            }

            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            return false;
        }
    }

    internal static ImmutableArray<string> LocateDatasets(IFileSystem fileSystem, string? projectFolder)
    {
        if (string.IsNullOrWhiteSpace(projectFolder) || !fileSystem.DirectoryExists(projectFolder))
        {
            return ImmutableArray<string>.Empty;
        }

        var datasetsFolder = Path.Combine(projectFolder, "OutMapper_InternalFiles", "Datasets");
        if (!fileSystem.DirectoryExists(datasetsFolder))
        {
            return ImmutableArray<string>.Empty;
        }

        var datasetFiles = fileSystem.GetFiles(datasetsFolder, "*.omds");
        if (datasetFiles.Length == 0)
        {
            return ImmutableArray<string>.Empty;
        }

        return datasetFiles
            .Select(file => Path.GetFileNameWithoutExtension(file)!)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToImmutableArray();
    }

    internal static bool CreateCohort(
        IFileSystem fileSystem, string? projectFolder, string cohortName,
        string? rawCsvFilePath, ImmutableArray<string> linkedDatasetNames)
    {
        if (string.IsNullOrWhiteSpace(projectFolder) || string.IsNullOrWhiteSpace(cohortName))
        {
            return false;
        }

        if (!fileSystem.DirectoryExists(projectFolder))
        {
            return false;
        }

        try
        {
            var cohortsFolder = Path.Combine(projectFolder, "OutMapper_InternalFiles", "Cohorts");
            fileSystem.CreateDirectory(cohortsFolder);

            var cohortFile = Path.Combine(cohortsFolder, cohortName + ".omch");
            if (fileSystem.FileExists(cohortFile))
            {
                return false;
            }

            fileSystem.CreateEmptyFile(cohortFile);

            var cohortFolder = Path.Combine(cohortsFolder, cohortName);
            var importedRawDataFolder = Path.Combine(cohortFolder, "Imported raw data");
            fileSystem.CreateDirectory(importedRawDataFolder);

            if (!string.IsNullOrWhiteSpace(rawCsvFilePath) && fileSystem.FileExists(rawCsvFilePath))
            {
                var destinationFile = Path.Combine(importedRawDataFolder, Path.GetFileName(rawCsvFilePath));
                fileSystem.CopyFile(rawCsvFilePath, destinationFile, overwrite: false);
            }

            var linkedDatasetsFile = Path.Combine(cohortFolder, "linked-datasets.json");
            fileSystem.WriteAllBytes(linkedDatasetsFile, JsonSerializer.SerializeToUtf8Bytes(linkedDatasetNames.ToArray()));

            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            return false;
        }
    }

    internal static ImmutableArray<string> LocateCohorts(IFileSystem fileSystem, string? projectFolder)
    {
        if (string.IsNullOrWhiteSpace(projectFolder) || !fileSystem.DirectoryExists(projectFolder))
        {
            return ImmutableArray<string>.Empty;
        }

        var cohortsFolder = Path.Combine(projectFolder, "OutMapper_InternalFiles", "Cohorts");
        if (!fileSystem.DirectoryExists(cohortsFolder))
        {
            return ImmutableArray<string>.Empty;
        }

        var cohortFiles = fileSystem.GetFiles(cohortsFolder, "*.omch");
        if (cohortFiles.Length == 0)
        {
            return ImmutableArray<string>.Empty;
        }

        return cohortFiles
            .Select(file => Path.GetFileNameWithoutExtension(file)!)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToImmutableArray();
    }

    internal static bool CreateAnalysis(IFileSystem fileSystem, string? projectFolder, string analysisName)
    {
        if (string.IsNullOrWhiteSpace(projectFolder) || string.IsNullOrWhiteSpace(analysisName))
        {
            return false;
        }

        if (!fileSystem.DirectoryExists(projectFolder))
        {
            return false;
        }

        try
        {
            var analysesFolder = Path.Combine(projectFolder, "OutMapper_InternalFiles", "Analyses");
            fileSystem.CreateDirectory(analysesFolder);

            var analysisFile = Path.Combine(analysesFolder, analysisName + ".oman");
            if (fileSystem.FileExists(analysisFile))
            {
                return false;
            }

            fileSystem.CreateEmptyFile(analysisFile);

            fileSystem.CreateDirectory(Path.Combine(analysesFolder, analysisName));

            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            return false;
        }
    }

    internal static ImmutableArray<string> LocateAnalyses(IFileSystem fileSystem, string? projectFolder)
    {
        if (string.IsNullOrWhiteSpace(projectFolder) || !fileSystem.DirectoryExists(projectFolder))
        {
            return ImmutableArray<string>.Empty;
        }

        var analysesFolder = Path.Combine(projectFolder, "OutMapper_InternalFiles", "Analyses");
        if (!fileSystem.DirectoryExists(analysesFolder))
        {
            return ImmutableArray<string>.Empty;
        }

        var analysisFiles = fileSystem.GetFiles(analysesFolder, "*.oman");
        if (analysisFiles.Length == 0)
        {
            return ImmutableArray<string>.Empty;
        }

        return analysisFiles
            .Select(file => Path.GetFileNameWithoutExtension(file)!)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToImmutableArray();
    }

    internal static bool CreateFigure(IFileSystem fileSystem, string? projectFolder, string figureName)
    {
        if (string.IsNullOrWhiteSpace(projectFolder) || string.IsNullOrWhiteSpace(figureName))
        {
            return false;
        }

        if (!fileSystem.DirectoryExists(projectFolder))
        {
            return false;
        }

        try
        {
            var figuresFolder = Path.Combine(projectFolder, "OutMapper_InternalFiles", "Figures");
            fileSystem.CreateDirectory(figuresFolder);

            var figureFile = Path.Combine(figuresFolder, figureName + ".omfg");
            if (fileSystem.FileExists(figureFile))
            {
                return false;
            }

            fileSystem.CreateEmptyFile(figureFile);

            fileSystem.CreateDirectory(Path.Combine(figuresFolder, figureName));

            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            return false;
        }
    }

    internal static ImmutableArray<string> LocateFigures(IFileSystem fileSystem, string? projectFolder)
    {
        if (string.IsNullOrWhiteSpace(projectFolder) || !fileSystem.DirectoryExists(projectFolder))
        {
            return ImmutableArray<string>.Empty;
        }

        var figuresFolder = Path.Combine(projectFolder, "OutMapper_InternalFiles", "Figures");
        if (!fileSystem.DirectoryExists(figuresFolder))
        {
            return ImmutableArray<string>.Empty;
        }

        var figureFiles = fileSystem.GetFiles(figuresFolder, "*.omfg");
        if (figureFiles.Length == 0)
        {
            return ImmutableArray<string>.Empty;
        }

        return figureFiles
            .Select(file => Path.GetFileNameWithoutExtension(file)!)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToImmutableArray();
    }

    public static async Task StopAsync()
    {
        _messageChannel.Writer.Complete();
        _cts.Cancel();
        if (_processingTask is not null)
        {
            await _processingTask;
        }
    }
}
