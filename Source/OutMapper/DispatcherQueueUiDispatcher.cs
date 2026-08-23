namespace OutMapper;

using Microsoft.UI.Dispatching;

/// <summary>Production <see cref="IUiDispatcher"/> backed by a real <see cref="DispatcherQueue"/>.</summary>
internal sealed class DispatcherQueueUiDispatcher : IUiDispatcher
{
    private readonly DispatcherQueue _dispatcherQueue;

    public DispatcherQueueUiDispatcher(DispatcherQueue dispatcherQueue)
    {
        _dispatcherQueue = dispatcherQueue;
    }

    public void Enqueue(Action action) => _dispatcherQueue.TryEnqueue(() => action());
}
