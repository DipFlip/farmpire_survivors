using UnityEngine;

/// <summary>
/// Shared state enum for all holdable items
/// </summary>
public enum ItemState { Pickup, Equipped }

/// <summary>
/// Interface for items that can be picked up and held by the player.
/// Implemented by WateringCan, Shovel, SeedBag, etc.
/// </summary>
public interface IHoldableItem
{
    /// <summary>
    /// Current state of the item (Pickup or Equipped)
    /// </summary>
    ItemState CurrentState { get; }

    /// <summary>
    /// The current target the item is aiming at (if any)
    /// </summary>
    Transform CurrentTarget { get; }

    /// <summary>
    /// Whether the item currently has a valid target
    /// </summary>
    bool HasTarget { get; }

    /// <summary>
    /// The tag used to find targets for this item (e.g., "Plant", "DigSite")
    /// </summary>
    string TargetTag { get; }

    /// <summary>
    /// The transform of this item's GameObject
    /// </summary>
    Transform Transform { get; }

    /// <summary>
    /// Equip this item to a holder at a given orbit angle
    /// </summary>
    void Equip(Transform holder, float orbitAngle);

    /// <summary>
    /// Drop this item at the specified position
    /// </summary>
    void Drop(Vector3 position);

    /// <summary>
    /// Set the movement direction for facing when not targeting
    /// </summary>
    void SetMoveDirection(Vector3 direction);

    /// <summary>
    /// Set the target orbit angle (for multi-item distribution)
    /// </summary>
    void SetTargetOrbitAngle(float angle);
}
