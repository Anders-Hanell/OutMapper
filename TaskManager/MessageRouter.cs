using Messages;

namespace TaskManager;

/// <summary>
/// Casts an incoming Message to its concrete subtype and calls the matching handler directly.
/// </summary>
internal static class MessageRouter
{
    internal static Task Route(Message message)
    {
        return message switch
        {
            WorkspaceChanged workspaceChanged => TaskManagerService.HandleWorkspaceChangedAsync(workspaceChanged),
            DatasetListRequest datasetListRequest => TaskManagerService.HandleDatasetListRequestAsync(datasetListRequest),
            CreateDatasetRequest createDatasetRequest => TaskManagerService.HandleCreateDatasetRequestAsync(createDatasetRequest),
            ParseDatasetRequest parseDatasetRequest => TaskManagerService.HandleParseDatasetRequestAsync(parseDatasetRequest),
            ParseResultRequest parseResultRequest => TaskManagerService.HandleParseResultRequestAsync(parseResultRequest),
            _ => Task.CompletedTask
        };
    }
}
