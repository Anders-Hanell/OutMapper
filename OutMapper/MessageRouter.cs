using System;
using Messages;
using Microsoft.UI.Dispatching;

namespace OutMapper;

public static class MessageRouter
{
    public static bool SendMessage(Message message)
    {
        return TaskManager.MessageRouter.SendMessage(message);
    }

    public static void ReceiveMessage(EventHandler<Message> handler)
    {
        // Captured here (rather than in a static field initializer) so it is guaranteed to be
        // resolved on the calling UI thread at the moment a real view actually subscribes,
        // instead of at some unspecified earlier point during type initialization.
        var uiDispatcher = DispatcherQueue.GetForCurrentThread();

        TaskManager.MessageRouter.ReceiveMessage((s, m) =>
        {
            uiDispatcher?.TryEnqueue(() => handler(s, m));
        });
    }
}
