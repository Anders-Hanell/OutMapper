using System;
using Messages;
using Microsoft.UI.Dispatching;

namespace OutMapper;

public static class MessageRouter
{
    private static readonly DispatcherQueue? _uiDispatcher = DispatcherQueue.GetForCurrentThread();

    public static bool SendMessage(Message message)
    {
        return TaskManager.MessageRouter.SendMessage(message);
    }

    public static void ReceiveMessage(EventHandler<Message> handler)
    {
        // Ensure handler is invoked on the UI dispatcher
        TaskManager.MessageRouter.ReceiveMessage((s, m) =>
        {
            _uiDispatcher?.TryEnqueue(() => handler(s, m));
        });
    }
}
