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
                ProjectDatasetsContent.Current?.OnDatasetListResponseReceived(listResponse);
                break;
            case CreateDatasetResponse createResponse:
                ProjectDatasetsContent.Current?.OnCreateDatasetResponseReceived(createResponse);
                break;
        }
    }
}
