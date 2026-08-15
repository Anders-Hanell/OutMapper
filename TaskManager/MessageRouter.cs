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
            CohortListRequest cohortListRequest => TaskManagerService.HandleCohortListRequestAsync(cohortListRequest),
            CreateCohortRequest createCohortRequest => TaskManagerService.HandleCreateCohortRequestAsync(createCohortRequest),
            ParseCohortRequest parseCohortRequest => TaskManagerService.HandleParseCohortRequestAsync(parseCohortRequest),
            CohortParseResultRequest cohortParseResultRequest => TaskManagerService.HandleCohortParseResultRequestAsync(cohortParseResultRequest),
            AnalysisListRequest analysisListRequest => TaskManagerService.HandleAnalysisListRequestAsync(analysisListRequest),
            CreateAnalysisRequest createAnalysisRequest => TaskManagerService.HandleCreateAnalysisRequestAsync(createAnalysisRequest),
            GenerateAnalysisGraphRequest generateAnalysisGraphRequest => TaskManagerService.HandleGenerateAnalysisGraphRequestAsync(generateAnalysisGraphRequest),
            AnalysisResultRequest analysisResultRequest => TaskManagerService.HandleAnalysisResultRequestAsync(analysisResultRequest),
            _ => Task.CompletedTask
        };
    }
}
