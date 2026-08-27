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
                ProjectAnalysisSettingsContent.Current?.OnCohortListResponseReceived(cohortListResponse);
                break;
            case CreateCohortResponse createCohortResponse:
                ProjectsPanel.Current?.OnCreateCohortResponseReceived(createCohortResponse);
                break;
            case CohortParseResultResponse cohortParseResultResponse:
                ProjectCohortContent.Current?.OnCohortParseResultResponseReceived(cohortParseResultResponse);
                break;
            case AnalysisListResponse analysisListResponse:
                ProjectsPanel.Current?.OnAnalysisListResponseReceived(analysisListResponse);
                break;
            case CreateAnalysisResponse createAnalysisResponse:
                ProjectsPanel.Current?.OnCreateAnalysisResponseReceived(createAnalysisResponse);
                break;
            case GenerateAnalysisGraphResponse generateAnalysisGraphResponse:
                ProjectAnalysisContent.Current?.OnGenerateAnalysisGraphResponseReceived(generateAnalysisGraphResponse);
                break;
            case AnalysisResultResponse analysisResultResponse:
                ProjectAnalysisContent.Current?.OnAnalysisResultResponseReceived(analysisResultResponse);
                break;
            case AnalysisSettingsResponse analysisSettingsResponse:
                ProjectAnalysisSettingsContent.Current?.OnAnalysisSettingsResponseReceived(analysisSettingsResponse);
                break;
            case ChannelListResponse channelListResponse:
                ProjectAnalysisSettingsContent.Current?.OnChannelListResponseReceived(channelListResponse);
                break;
            case FigureListResponse figureListResponse:
                ProjectsPanel.Current?.OnFigureListResponseReceived(figureListResponse);
                break;
            case CreateFigureResponse createFigureResponse:
                ProjectsPanel.Current?.OnCreateFigureResponseReceived(createFigureResponse);
                break;
            case FigureLayoutResponse figureLayoutResponse:
                ProjectFigureContent.Current?.OnFigureLayoutResponseReceived(figureLayoutResponse);
                break;
            case SaveFigureSizeResponse saveFigureSizeResponse:
                ProjectFigureContent.Current?.OnSaveFigureSizeResponseReceived(saveFigureSizeResponse);
                break;
            case AnalysesWithGraphListResponse analysesWithGraphListResponse:
                ProjectFigureSelectGraphsContent.Current?.OnAnalysesWithGraphListResponseReceived(analysesWithGraphListResponse);
                break;
            case CreateFigureGraphResponse createFigureGraphResponse:
                ProjectFigureContent.Current?.OnCreateFigureGraphResponseReceived(createFigureGraphResponse);
                break;
            case RenderFigurePreviewResponse renderFigurePreviewResponse:
                ProjectFigureSelectGraphsContent.Current?.OnRenderFigurePreviewResponseReceived(renderFigurePreviewResponse);
                break;
        }
    }
}
