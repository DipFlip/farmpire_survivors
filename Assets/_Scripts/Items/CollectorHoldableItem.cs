using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Abstract base class for holdable items that collect CollectableItems.
/// Does NOT fire projectiles. Detects collectables in range and pulls them in.
///
/// Subclasses can customize collection behavior and sounds.
/// </summary>
public abstract class CollectorHoldableItem : HoldableItemBase
{
    [Header("Collection Settings")]
    [Tooltip("Range to detect collectables")]
    [SerializeField] protected float collectionRange = 3f;

    [Tooltip("Maximum items this collector can hold")]
    [SerializeField] protected int maxCapacity = 6;

    [Tooltip("Delay between pulling items")]
    [SerializeField] protected float pullDelay = 0.2f;

    // Runtime
    protected int currentCount = 0;
    protected List<CollectableItem> beingCollected = new List<CollectableItem>();
    protected float lastPullTime;

    /// <summary>
    /// Number of items currently collected
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

    // Override targeting - collectors don't target anything specific
    // They detect collectables differently
    protected override string TargetTag => "Collectable";

    protected override bool IsValidTarget(GameObject targetObj)
    {
        CollectableItem item = targetObj.GetComponent<CollectableItem>();
        return item != null && item.CanBeCollected;
    }

    protected override void Update()
    {
        // Call base for orbit and rotation, but skip target finding
        if (currentState != ItemState.Equipped || holder == null) return;

        UpdateOrbitPosition();
        UpdateRotation();

        // Collector-specific logic
        PullCollectables();
        CheckCollectionProgress();
    }

    protected virtual void PullCollectables()
    {
        if (!HasSpace) return;
        if (Time.time < lastPullTime + pullDelay) return;

        // Find collectables in range
        Collider[] colliders = Physics.OverlapSphere(transform.position, collectionRange);

        foreach (var col in colliders)
        {
            if (!col.CompareTag("Collectable")) continue;

            CollectableItem item = col.GetComponent<CollectableItem>();
            if (item == null || !item.CanBeCollected) continue;
            if (beingCollected.Contains(item)) continue;

            // Check if we still have space (accounting for items being collected)
            int pendingCount = currentCount + beingCollected.Count;
            if (pendingCount >= maxCapacity) break;

            // Start collecting this item
            item.StartCollection(transform);
            beingCollected.Add(item);
            lastPullTime = Time.time;

            // Pull one at a time for nicer visual
            break;
        }
    }

    protected virtual void CheckCollectionProgress()
    {
        for (int i = beingCollected.Count - 1; i >= 0; i--)
        {
            CollectableItem item = beingCollected[i];

            if (item == null || item.State == CollectableItem.CollectableState.Collected)
            {
                // Item was collected
                beingCollected.RemoveAt(i);
                OnItemCollected();
            }
            else if (item.State == CollectableItem.CollectableState.Idle)
            {
                // Collection was cancelled
                beingCollected.RemoveAt(i);
            }
        }
    }

    protected virtual void OnItemCollected()
    {
        currentCount++;
        PlayPulse();
        PlayActionSound();
    }

    public override void Drop(Vector3 position)
    {
        // Cancel any in-progress collections
        foreach (var item in beingCollected)
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
    /// Empty the collector (for depositing items)
    /// </summary>
    public virtual void EmptyCollector()
    {
        currentCount = 0;
    }

    /// <summary>
    /// Remove a specific number of items
    /// </summary>
    public virtual int RemoveItems(int count)
    {
        int removed = Mathf.Min(count, currentCount);
        currentCount -= removed;
        return removed;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        // Draw collection range
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, collectionRange);
    }
}
