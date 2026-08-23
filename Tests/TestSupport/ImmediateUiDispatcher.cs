using OutMapper;

namespace TestSupport;

/// <summary>
/// <see cref="IUiDispatcher"/> fake that runs the action immediately on the calling thread, so tests
/// don't need a live UI dispatcher thread to observe messages routed back from TaskManager.
/// </summary>
public sealed class ImmediateUiDispatcher : IUiDispatcher
{
    public void Enqueue(Action action) => action();
}
