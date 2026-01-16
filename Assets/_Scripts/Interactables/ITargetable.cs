/// <summary>
/// Interface for objects that can be targeted by holdable items.
/// Implemented by Plant, DigSite, etc.
/// </summary>
public interface ITargetable
{
    /// <summary>
    /// Whether this target can currently receive input (water, dig, seeds, etc.)
    /// </summary>
    bool CanReceive { get; }

    /// <summary>
    /// Receive a generic amount of input. The meaning depends on the target type.
    /// Plants receive water, DigSites receive dig damage or seeds.
    /// </summary>
    void ReceiveAmount(float amount);
}
