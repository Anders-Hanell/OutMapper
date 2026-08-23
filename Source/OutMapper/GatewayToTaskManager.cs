namespace OutMapper;

using Messages;
using Microsoft.UI.Dispatching;
using TaskManager;

/// <summary>
/// The sole crossing point from OutMapper to TaskManager. OutMapper references TaskManager
/// directly, so outbound messages call straight into it; inbound messages arrive via the
/// registered <see cref="IGatewayReceiver"/> callback and are dispatched onto the UI thread
/// before being handed to the OutMapper MessageRouter.
/// </summary>
public static class GatewayToTaskManager
{
    private static readonly Receiver receiver = new();
    private static IUiDispatcher? uiDispatcher;
    private static Action<Message> onMessageReceived = MessageRouter.Route;

    /// <summary>
    /// Must be called once from the UI thread during app startup, so the dispatcher used to
    /// marshal incoming messages back to the UI thread is captured deterministically rather
    /// than at some unspecified point during type initialization.
    /// </summary>
    public static void Initialize()
    {
        var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        Initialize(dispatcherQueue is null ? null : new DispatcherQueueUiDispatcher(dispatcherQueue), MessageRouter.Route);
    }

    /// <summary>
    /// Test-only seam: lets a unit test supply a synchronous <see cref="IUiDispatcher"/> and capture
    /// routed messages directly, instead of requiring a live UI dispatcher thread and the real
    /// MessageRouter targets (which are live Uno screens).
    /// </summary>
    internal static void Initialize(IUiDispatcher? dispatcher, Action<Message> onMessageReceived)
    {
        uiDispatcher = dispatcher;
        GatewayToTaskManager.onMessageReceived = onMessageReceived;
        GatewayToOutMapper.Receiver = receiver;
    }

    /// <summary>Send a message from OutMapper out to TaskManager.</summary>
    public static void SendMessage(Message message)
    {
        GatewayToOutMapper.ReceiveMessage(message);
    }

    /// <summary>Receive a message coming in from TaskManager, crossing back onto the UI thread.</summary>
    private static void ReceiveMessage(Message message)
    {
        uiDispatcher?.Enqueue(() => onMessageReceived(message));
    }

    private sealed class Receiver : IGatewayReceiver
    {
        public void ReceiveMessage(Message message) => GatewayToTaskManager.ReceiveMessage(message);
    }
}
