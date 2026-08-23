namespace OutMapper;

/// <summary>
/// Seam over <see cref="Microsoft.UI.Dispatching.DispatcherQueue"/> so <see cref="GatewayToTaskManager"/>
/// can be unit tested without a live UI dispatcher thread.
/// </summary>
internal interface IUiDispatcher
{
    void Enqueue(Action action);
}
