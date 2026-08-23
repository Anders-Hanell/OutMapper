namespace OutMapper;

using System.Collections.Concurrent;
using Messages;

/// <summary>
/// Matches a correlated request sent through <see cref="GatewayToTaskManager"/> to its reply, for the
/// few flows (pure computations like heatmap/figure layout) that need to await one specific response
/// rather than have it routed by type to a screen singleton, which is how every other flow works.
/// <see cref="MessageRouter"/> feeds replies here via <see cref="TryComplete"/>.
/// </summary>
internal static class GatewayRequestCorrelator
{
    private static readonly ConcurrentDictionary<Guid, TaskCompletionSource<Message>> pendingRequests = new();

    internal static Task<TResponse> SendAsync<TRequest, TResponse>(TRequest request)
        where TRequest : Message, ICorrelatedMessage
        where TResponse : Message
    {
        var tcs = new TaskCompletionSource<Message>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!pendingRequests.TryAdd(request.RequestId, tcs))
        {
            throw new InvalidOperationException($"A request with id {request.RequestId} is already pending.");
        }

        GatewayToTaskManager.SendMessage(request);
        return AwaitResponse<TResponse>(tcs.Task, request.RequestId);
    }

    private static async Task<TResponse> AwaitResponse<TResponse>(Task<Message> pendingReply, Guid requestId)
        where TResponse : Message
    {
        try
        {
            return (TResponse)await pendingReply;
        }
        finally
        {
            pendingRequests.TryRemove(requestId, out _);
        }
    }

    /// <summary>Completes the pending request matching <paramref name="response"/>'s id, if any is still awaited.</summary>
    internal static void TryComplete(ICorrelatedMessage response)
    {
        if (pendingRequests.TryGetValue(response.RequestId, out var tcs))
        {
            tcs.TrySetResult((Message)response);
        }
    }
}
