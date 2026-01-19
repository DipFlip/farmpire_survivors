using UnityEngine;

/// <summary>
/// Attach to the player to pick up and manage holdable items.
/// Only ONE item can be held at a time. Picking up a new item swaps with the current one.
///
/// Setup: Add a SphereCollider (isTrigger=true) as a child object for pickup detection,
/// or this script will create one automatically.
/// </summary>
public class ItemHolder : MonoBehaviour
{
    [Header("Pickup")]
    [SerializeField] private float pickupRadius = 2f;
    [SerializeField] private float swapCooldown = 0.5f;
    [Tooltip("Optional: Assign existing trigger collider. If empty, one will be created.")]
    [SerializeField] private SphereCollider pickupTrigger;

    [Header("Movement")]
    [Tooltip("If assigned, uses this for movement direction. Otherwise calculates from position delta.")]
    [SerializeField] private CharacterController characterController;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    // Single item only
    private IHoldableItem equippedItem;
    private Vector3 lastPosition;
    private Vector3 moveDirection;
    private float lastSwapTime;

    /// <summary>
    /// The currently equipped item, or null if none
    /// </summary>
    public IHoldableItem EquippedItem => equippedItem;

    /// <summary>
    /// Whether the player is currently holding an item
    /// </summary>
    public bool HasItem => equippedItem != null;

    private void Awake()
    {
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }
        lastPosition = transform.position;

        SetupPickupTrigger();
    }

    private void SetupPickupTrigger()
    {
        if (pickupTrigger == null)
        {
            GameObject triggerObj = new GameObject("PickupTrigger");
            triggerObj.transform.SetParent(transform);
            triggerObj.transform.localPosition = Vector3.zero;

            pickupTrigger = triggerObj.AddComponent<SphereCollider>();
            pickupTrigger.isTrigger = true;
            pickupTrigger.radius = pickupRadius;

            Rigidbody rb = triggerObj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            PickupTriggerHandler handler = triggerObj.AddComponent<PickupTriggerHandler>();
            handler.Initialize(this);
        }
        else
        {
            PickupTriggerHandler handler = pickupTrigger.GetComponent<PickupTriggerHandler>();
            if (handler == null)
            {
                handler = pickupTrigger.gameObject.AddComponent<PickupTriggerHandler>();
            }
            handler.Initialize(this);

            if (pickupTrigger.GetComponent<Rigidbody>() == null)
            {
                Rigidbody rb = pickupTrigger.gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }
    }

    private void Update()
    {
        UpdateMoveDirection();
        UpdateItem();
    }

    private void UpdateMoveDirection()
    {
        if (characterController != null && characterController.velocity.sqrMagnitude > 0.01f)
        {
            Vector3 vel = characterController.velocity;
            vel.y = 0;
            if (vel.sqrMagnitude > 0.01f)
            {
                moveDirection = vel.normalized;
            }
        }
        else
        {
            Vector3 delta = transform.position - lastPosition;
            delta.y = 0;
            if (delta.sqrMagnitude > 0.0001f)
            {
                moveDirection = delta.normalized;
            }
        }

        lastPosition = transform.position;
    }

    /// <summary>
    /// Called by PickupTriggerHandler when an item enters pickup range
    /// </summary>
    public void OnItemEnteredRange(IHoldableItem item)
    {
        if (item == null || item.CurrentState != ItemState.Pickup) return;

        // Cooldown to prevent rapid swapping
        if (Time.time < lastSwapTime + swapCooldown) return;

        PickupItem(item, item.Transform.position);
    }

    private void PickupItem(IHoldableItem newItem, Vector3 newItemPosition)
    {
        // SWAP LOGIC: If already holding an item, drop it at the new item's position
        if (equippedItem != null)
        {
            equippedItem.Drop(newItemPosition);
            lastSwapTime = Time.time;
        }

        equippedItem = newItem;
        equippedItem.Equip(transform, 0f);
    }

    /// <summary>
    /// Drop the currently held item at the player's position
    /// </summary>
    public void DropCurrentItem()
    {
        if (equippedItem == null) return;

        equippedItem.Drop(transform.position);
        equippedItem = null;
    }

    private void UpdateItem()
    {
        if (equippedItem != null)
        {
            equippedItem.SetMoveDirection(moveDirection);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, pickupRadius);

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position + Vector3.up, moveDirection * 2f);
        }
    }
}

/// <summary>
/// Helper component that forwards trigger events to ItemHolder.
/// </summary>
public class PickupTriggerHandler : MonoBehaviour
{
    private ItemHolder itemHolder;

    public void Initialize(ItemHolder holder)
    {
        itemHolder = holder;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (itemHolder == null) return;

        IHoldableItem item = null;

        // First check for PickupCollider marker (explicit pickup zone)
        PickupCollider pickupCollider = other.GetComponent<PickupCollider>();
        if (pickupCollider != null)
        {
            item = pickupCollider.Item;
        }
        else
        {
            // Fallback: check if this collider belongs to an item without a PickupCollider marker
            item = other.GetComponent<IHoldableItem>();
            if (item == null) item = other.GetComponentInParent<IHoldableItem>();

            // If item has a PickupCollider somewhere, only that collider should trigger pickup
            if (item != null && item.Transform.GetComponentInChildren<PickupCollider>() != null)
            {
                return; // This collider isn't the designated pickup collider
            }
        }

        if (item != null)
        {
            itemHolder.OnItemEnteredRange(item);
        }
    }
}
