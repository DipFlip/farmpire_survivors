using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Abstract base class for holdable items that collect CollectableItems.
/// Does NOT fire projectiles. Detects collectables in range and pulls them in.
/// Tracks collected items by type for use with deposit stations.
/// Also detects DepositStations and transfers items to them over time.
///
/// Subclasses can customize collection behavior and sounds.
/// Requires Rigidbody for trigger detection.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public abstract class CollectorHoldableItem : HoldableItemBase
{
    [Header("Collection Settings")]
    [Tooltip("Range to detect collectables")]
    [SerializeField] protected float collectionRange = 3f;

    [Tooltip("Maximum total items this collector can hold")]
    [SerializeField] protected int maxCapacity = 6;

    [Tooltip("Delay between pulling collectables")]
    [SerializeField] protected float pullDelay = 0.2f;

    [Header("Deposit Settings")]
    [Tooltip("Time between depositing each item")]
    [SerializeField] protected float depositInterval = 0.5f;

    // Runtime - track items by type
    protected Dictionary<string, int> collectedItems = new Dictionary<string, int>();
    protected int currentCount = 0;

    // Track items being collected (need to store type before item is destroyed)
    protected List<(CollectableItem item, string itemType)> beingCollected = new List<(CollectableItem, string)>();
    protected float lastPullTime;

    // Deposit tracking
    protected DepositStation currentDepositStation;
    protected float lastDepositTime;

    /// <summary>
    /// Total number of items currently collected
    /// </summary>
    public int CurrentCount => currentCount;

    /// <summary>
    /// Maximum capacity
    /// </summary>
    public int MaxCapacity => maxCapacity;

    /// <summary>
    /// Whether collector has space for more items
    /// </summary>
    public bool HasSpace => currentCount < maxCapacity;

    /// <summary>
    /// How full the collector is (0 to 1)
    /// </summary>
    public float FillProgress => maxCapacity > 0 ? (float)currentCount / maxCapacity : 0f;

    /// <summary>
    /// Get a read-only view of collected items by type
    /// </summary>
    public IReadOnlyDictionary<string, int> CollectedItems => collectedItems;

    // Override targeting - collectors don't target anything specific
    protected override string TargetTag => "Collectable";

    protected override void Awake()
    {
        base.Awake();

        // Setup Rigidbody for trigger detection (required for trigger-to-trigger)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    protected override bool IsValidTarget(GameObject targetObj)
    {
        CollectableItem item = targetObj.GetComponent<CollectableItem>();
        return item != null && item.CanBeCollected;
    }

    protected override void Update()
    {
        if (currentState != ItemState.Equipped || holder == null) return;

        UpdateOrbitPosition();
        UpdateRotation();

        PullCollectables();
        CheckCollectionProgress();
        TryDepositToStation();
    }

    /// <summary>
    /// Called by DepositDetector child when entering a DepositStation
    /// </summary>
    public virtual void OnDepositStationEnter(DepositStation station)
    {
        if (station != null && !station.IsComplete)
        {
            currentDepositStation = station;
            Debug.Log($"[Collector] Entered deposit range: {station.name}");
        }
    }

    /// <summary>
    /// Called by DepositDetector child when exiting a DepositStation
    /// </summary>
    public virtual void OnDepositStationExit(DepositStation station)
    {
        if (station != null && station == currentDepositStation)
        {
            currentDepositStation = null;
            Debug.Log($"[Collector] Exited deposit range: {station.name}");
        }
    }

    protected virtual void TryDepositToStation()
    {
        if (currentDepositStation == null) return;
        if (currentDepositStation.IsComplete)
        {
            currentDepositStation = null;
            return;
        }
        if (currentCount <= 0) return;
        if (Time.time < lastDepositTime + depositInterval) return;

        // Find an item type we have that the station needs
        foreach (var req in currentDepositStation.Requirements)
        {
            if (req.currentAmount >= req.amount) continue; // Already fulfilled

            if (collectedItems.TryGetValue(req.itemType, out int have) && have > 0)
            {
                // Transfer one item
                RemoveItems(req.itemType, 1);
                currentDepositStation.ReceiveItem(req.itemType, 1);
                lastDepositTime = Time.time;

                PlayPulse();
                OnItemDeposited(req.itemType);

                Debug.Log($"[Collector] Deposited 1x {req.itemType}");
                return; // One item per interval
            }
        }
    }

    protected virtual void OnItemDeposited(string itemType)
    {
        // Override in subclass for sound/effects
    }

    protected virtual void PullCollectables()
    {
        if (!HasSpace) return;
        if (Time.time < lastPullTime + pullDelay) return;

        Collider[] colliders = Physics.OverlapSphere(transform.position, collectionRange);

        foreach (var col in colliders)
        {
            if (!col.CompareTag("Collectable")) continue;

            CollectableItem item = col.GetComponent<CollectableItem>();
            if (item == null || !item.CanBeCollected) continue;

            // Check if already being collected
            bool alreadyCollecting = false;
            foreach (var pair in beingCollected)
            {
                if (pair.item == item)
                {
                    alreadyCollecting = true;
                    break;
                }
            }
            if (alreadyCollecting) continue;

            // Check if we still have space
            int pendingCount = currentCount + beingCollected.Count;
            if (pendingCount >= maxCapacity) break;

            // Start collecting - store type now before item is destroyed
            item.StartCollection(transform);
            beingCollected.Add((item, item.ItemType));
            lastPullTime = Time.time;

            // Pull one at a time for nicer visual
            break;
        }
    }

    protected virtual void CheckCollectionProgress()
    {
        for (int i = beingCollected.Count - 1; i >= 0; i--)
        {
            var (item, itemType) = beingCollected[i];

            if (item == null || item.State == CollectableItem.CollectableState.Collected)
            {
                // Item was collected - add to inventory by type
                beingCollected.RemoveAt(i);
                OnItemCollected(itemType);
            }
            else if (item.State == CollectableItem.CollectableState.Idle)
            {
                // Collection was cancelled
                beingCollected.RemoveAt(i);
            }
        }
    }

    protected virtual void OnItemCollected(string itemType)
    {
        // Add to type-specific count
        if (collectedItems.ContainsKey(itemType))
        {
            collectedItems[itemType]++;
        }
        else
        {
            collectedItems[itemType] = 1;
        }

        currentCount++;
        PlayPulse();
        PlayActionSound();
    }

    public override void Drop(Vector3 position)
    {
        // Cancel any in-progress collections
        foreach (var (item, _) in beingCollected)
        {
            if (item != null)
            {
                item.CancelCollection();
            }
        }
        beingCollected.Clear();

        base.Drop(position);
    }

    /// <summary>
    /// Get count of a specific item type
    /// </summary>
    public int GetItemCount(string itemType)
    {
        return collectedItems.TryGetValue(itemType, out int count) ? count : 0;
    }

    /// <summary>
    /// Check if collector has at least the specified amount of an item type
    /// </summary>
    public bool HasItems(string itemType, int amount)
    {
        return GetItemCount(itemType) >= amount;
    }

    /// <summary>
    /// Check if collector has all required items (for deposit stations)
    /// </summary>
    public bool HasAllItems(Dictionary<string, int> requirements)
    {
        foreach (var req in requirements)
        {
            if (GetItemCount(req.Key) < req.Value)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Remove specific amount of an item type. Returns actual amount removed.
    /// </summary>
    public int RemoveItems(string itemType, int amount)
    {
        if (!collectedItems.TryGetValue(itemType, out int current))
        {
            return 0;
        }

        int toRemove = Mathf.Min(amount, current);
        collectedItems[itemType] = current - toRemove;
        currentCount -= toRemove;

        // Clean up empty entries
        if (collectedItems[itemType] <= 0)
        {
            collectedItems.Remove(itemType);
        }

        return toRemove;
    }

    /// <summary>
    /// Remove multiple item types at once (for deposit stations)
    /// Returns true if all items were removed, false if requirements not met
    /// </summary>
    public bool RemoveItems(Dictionary<string, int> requirements)
    {
        // First check if we have everything
        if (!HasAllItems(requirements))
        {
            return false;
        }

        // Remove all
        foreach (var req in requirements)
        {
            RemoveItems(req.Key, req.Value);
        }

        return true;
    }

    /// <summary>
    /// Empty the collector completely
    /// </summary>
    public virtual void EmptyCollector()
    {
        collectedItems.Clear();
        currentCount = 0;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, collectionRange);
    }
}
