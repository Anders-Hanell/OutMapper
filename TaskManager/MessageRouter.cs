using Messages;

namespace TaskManager;

public static class MessageRouter
{
    public static event EventHandler<Message>? MessageReceived;

    public static bool SendMessage(Message message)
    {
        return TaskManagerService.EnqueueMessage(message);
    }

    public static void ReceiveMessage(EventHandler<Message> handler)
    {
        MessageReceived += handler;
    }

    internal static void Emit(Message message)
    {
        MessageReceived?.Invoke(null, message);
    }
}
