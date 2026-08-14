using System.Collections.Immutable;
using System.IO;
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
    private static string? _workspaceFolder;

    public static string? CurrentWorkspaceFolder => _workspaceFolder;

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

    internal static Task HandleWorkspaceChangedAsync(WorkspaceChanged message)
    {
        _workspaceFolder = message.WorkspaceFolder;
        return Task.CompletedTask;
    }

    internal static Task HandleDatasetListRequestAsync(DatasetListRequest message)
    {
        var datasetNames = LocateDatasets(_workspaceFolder, message.ProjectName);

        var response = new DatasetListResponse(message.ProjectName, datasetNames);

        GatewayToOutMapper.SendMessage(response);
        return Task.CompletedTask;
    }

    internal static Task HandleCreateDatasetRequestAsync(CreateDatasetRequest message)
    {
        var createdDataset = CreateDataset(_workspaceFolder, message.ProjectName, message.DatasetName, message.RawDataFolderPath);

        var response = new CreateDatasetResponse(message.DatasetName, message.ProjectName, createdDataset);

        GatewayToOutMapper.SendMessage(response);
        return Task.CompletedTask;
    }

    private static bool CreateDataset(string? workspaceFolder, string? projectName, string datasetName, string? rawDataFolderPath)
    {
        if (string.IsNullOrWhiteSpace(workspaceFolder) || string.IsNullOrWhiteSpace(projectName) ||
            string.IsNullOrWhiteSpace(datasetName))
        {
            return false;
        }

        var projectFolder = Path.Combine(workspaceFolder, "Projects", projectName);
        if (!Directory.Exists(projectFolder))
        {
            return false;
        }

        try
        {
            var datasetsFolder = Path.Combine(projectFolder, "OutMapper_InternalFiles", "Datasets");
            Directory.CreateDirectory(datasetsFolder);

            var datasetFile = Path.Combine(datasetsFolder, datasetName + ".omds");
            if (File.Exists(datasetFile))
            {
                return false;
            }

            using (File.Create(datasetFile))
            {
            }

            var datasetFolder = Path.Combine(datasetsFolder, datasetName);
            var importedRawDataFolder = Path.Combine(datasetFolder, "Imported raw data");
            Directory.CreateDirectory(importedRawDataFolder);

            if (!string.IsNullOrWhiteSpace(rawDataFolderPath) && Directory.Exists(rawDataFolderPath))
            {
                foreach (var csvFile in Directory.GetFiles(rawDataFolderPath, "*.csv", SearchOption.TopDirectoryOnly))
                {
                    var destinationFile = Path.Combine(importedRawDataFolder, Path.GetFileName(csvFile));
                    File.Copy(csvFile, destinationFile, overwrite: false);
                }
            }

            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            return false;
        }
    }

    private static ImmutableArray<string> LocateDatasets(string? workspaceFolder, string? projectName)
    {
        if (string.IsNullOrWhiteSpace(workspaceFolder) || !Directory.Exists(workspaceFolder) ||
            string.IsNullOrWhiteSpace(projectName))
        {
            return ImmutableArray<string>.Empty;
        }

        var datasetsFolder = Path.Combine(workspaceFolder, "Projects", projectName, "OutMapper_InternalFiles", "Datasets");
        if (!Directory.Exists(datasetsFolder))
        {
            return ImmutableArray<string>.Empty;
        }

        var datasetFiles = Directory.GetFiles(datasetsFolder, "*.omds", SearchOption.TopDirectoryOnly);
        if (datasetFiles.Length == 0)
        {
            return ImmutableArray<string>.Empty;
        }

        return datasetFiles
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
