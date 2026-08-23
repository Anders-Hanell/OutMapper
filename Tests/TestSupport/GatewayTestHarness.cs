using OutMapper;

namespace TestSupport;

/// <summary>
/// Wires up <see cref="GatewayToTaskManager"/> once per test process with a synchronous
/// <see cref="ImmediateUiDispatcher"/> and the real <c>MessageRouter.Route</c>, so tests can exercise
/// flows that round-trip through the gateway (e.g. correlated layout requests) without a live UI thread.
/// </summary>
public static class GatewayTestHarness
{
    private static bool initialized;

    public static void EnsureInitialized()
    {
        if (initialized)
        {
            return;
        }

        GatewayToTaskManager.Initialize(new ImmediateUiDispatcher(), MessageRouter.Route);
        initialized = true;
    }
}
