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
            await ProcessMessageAsync(message);
        }
    }

    private static Task ProcessMessageAsync(Message message)
    {
        return message switch
        {
            WorkspaceChanged workspaceChanged => HandleWorkspaceChangedAsync(workspaceChanged),
            DatasetListRequest datasetListRequest => HandleDatasetListRequestAsync(datasetListRequest),
            CreateDatasetRequest createDatasetRequest => HandleCreateDatasetRequestAsync(createDatasetRequest),
            _ => Task.CompletedTask
        };
    }

    private static Task HandleWorkspaceChangedAsync(WorkspaceChanged message)
    {
        _workspaceFolder = message.WorkspaceFolder;
        return Task.CompletedTask;
    }

    private static Task HandleDatasetListRequestAsync(DatasetListRequest message)
    {
        var workspaceFolder = message.WorkspaceFolder ?? _workspaceFolder;
        var datasetNames = LocateDatasets(workspaceFolder, message.ProjectName);

        var response = new DatasetListResponse(message.ProjectName, datasetNames);

        MessageRouter.Emit(response);
        return Task.CompletedTask;
    }

    private static Task HandleCreateDatasetRequestAsync(CreateDatasetRequest message)
    {
        var workspaceFolder = message.WorkspaceFolder ?? _workspaceFolder;
        var createdDataset = CreateDataset(workspaceFolder, message.ProjectName, message.DatasetName);

        var response = new CreateDatasetResponse(message.DatasetName, message.ProjectName, createdDataset);

        MessageRouter.Emit(response);
        return Task.CompletedTask;
    }

    private static bool CreateDataset(string? workspaceFolder, string? projectName, string datasetName)
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

        var datasetsFolder = Path.Combine(projectFolder, "Datasets");
        Directory.CreateDirectory(datasetsFolder);

        var datasetFile = Path.Combine(datasetsFolder, datasetName + ".omds");
        if (File.Exists(datasetFile))
        {
            return false;
        }

        using var stream = File.Create(datasetFile);
        return true;
    }

    private static ImmutableArray<string> LocateDatasets(string? workspaceFolder, string? projectName)
    {
        if (string.IsNullOrWhiteSpace(workspaceFolder) || !Directory.Exists(workspaceFolder) ||
            string.IsNullOrWhiteSpace(projectName))
        {
            return ImmutableArray<string>.Empty;
        }

        var datasetsFolder = Path.Combine(workspaceFolder, "Projects", projectName, "Datasets");
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
