using UnityEngine;

/// <summary>
/// Marker component that designates this collider as the pickup trigger for an item.
/// Attach to the specific collider that should trigger item pickup.
/// If an item has this component on any child, only that collider will trigger pickup.
/// </summary>
public class PickupCollider : MonoBehaviour
{
    private IHoldableItem cachedItem;

    public IHoldableItem Item
    {
        get
        {
            if (cachedItem == null)
            {
                cachedItem = GetComponentInParent<IHoldableItem>();
            }
            return cachedItem;
        }
    }
}
