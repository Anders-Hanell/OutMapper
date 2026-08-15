using Messages;

namespace OutMapper;

/// <summary>
/// Casts an incoming Message to its concrete subtype and calls the matching handler directly.
/// </summary>
public static class MessageRouter
{
    public static void SendMessage(Message message)
    {
        GatewayToTaskManager.SendMessage(message);
    }

    internal static void Route(Message message)
    {
        switch (message)
        {
            case DatasetListResponse listResponse:
                ProjectsPanel.Current?.OnDatasetListResponseReceived(listResponse);
                ProjectCreateCohortContent.Current?.OnDatasetListResponseReceived(listResponse);
                break;
            case CreateDatasetResponse createResponse:
                ProjectsPanel.Current?.OnCreateDatasetResponseReceived(createResponse);
                break;
            case ParseResultResponse parseResultResponse:
                ProjectDatasetContent.Current?.OnParseResultResponseReceived(parseResultResponse);
                break;
            case CohortListResponse cohortListResponse:
                ProjectsPanel.Current?.OnCohortListResponseReceived(cohortListResponse);
                break;
            case CreateCohortResponse createCohortResponse:
                ProjectsPanel.Current?.OnCreateCohortResponseReceived(createCohortResponse);
                break;
            case CohortParseResultResponse cohortParseResultResponse:
                ProjectCohortContent.Current?.OnCohortParseResultResponseReceived(cohortParseResultResponse);
                break;
        }
    }
}
