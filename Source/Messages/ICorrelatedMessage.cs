namespace Messages;

/// <summary>
/// A message that carries a caller-supplied id so the reply can be matched to the specific request that
/// caused it, rather than being routed by type to a single screen (how every other message flows).
/// </summary>
public interface ICorrelatedMessage
{
    Guid RequestId { get; }
}
