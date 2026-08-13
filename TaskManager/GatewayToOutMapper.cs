namespace TaskManager;

using Messages;

/// <summary>
/// The sole crossing point from TaskManager to OutMapper. TaskManager has no project
/// reference to OutMapper, so outbound messages are delivered through a receiver that
/// OutMapper registers at startup.
/// </summary>
public static class GatewayToOutMapper
{
    public static IGatewayReceiver? Receiver { get; set; }

    /// <summary>Send a message from TaskManager out to OutMapper.</summary>
    public static void SendMessage(Message message)
    {
        Receiver?.ReceiveMessage(message);
    }

    /// <summary>Receive a message coming in from OutMapper, crossing onto TaskManager's processing thread.</summary>
    public static void ReceiveMessage(Message message)
    {
        TaskManagerService.EnqueueMessage(message);
    }
}
