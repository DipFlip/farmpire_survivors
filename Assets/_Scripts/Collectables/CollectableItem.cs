using UnityEngine;
using DG.Tweening;

/// <summary>
/// An item that can be collected by a Collector.
/// Spawned by Trees when chopped, or other sources.
/// Has Rigidbody for physics scatter on spawn.
/// When collected, animates flying toward Collector and shrinks.
///
/// Setup:
/// - Add "Collectable" tag
/// - Set itemType to identify this collectable (e.g., "log", "tomato", "rock")
/// - Requires Rigidbody component
/// - Requires Collider component
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class CollectableItem : MonoBehaviour
{
    public enum CollectableState
    {
        Idle,           // Waiting to be collected
        BeingCollected, // Flying toward collector
        Collected       // Collection complete, about to be destroyed
    }

    [Header("Item Type")]
    [Tooltip("Identifier for this collectable type (e.g., 'log', 'tomato', 'rock')")]
    [SerializeField] private string itemType = "item";

    /// <summary>
    /// The type identifier for this collectable
    /// </summary>
    public string ItemType => itemType;

    [Header("Collection Animation")]
    [Tooltip("Time to fly toward collector")]
    [SerializeField] private float collectDuration = 0.5f;

    [Tooltip("Time to shrink at end of collection")]
    [SerializeField] private float shrinkDuration = 0.2f;

    [Header("Physics")]
    [Tooltip("Time after spawn before item can be collected")]
    [SerializeField] private float settleTime = 1f;

    [Header("Visual")]
    [Tooltip("Bob up/down amplitude when settled")]
    [SerializeField] private float bobAmplitude = 0.1f;

    [Tooltip("Bob frequency in Hz")]
    [SerializeField] private float bobFrequency = 2f;

    [Tooltip("Rotate speed when idle (degrees/sec)")]
    [SerializeField] private float idleRotateSpeed = 90f;

    private Rigidbody rb;
    private Collider col;
    private CollectableState state = CollectableState.Idle;
    private float spawnTime;
    private Vector3 originalScale;
    private Transform collectTarget;
    private float collectStartTime;
    private Vector3 collectStartPos;
    private Tween collectTween;

    // Idle animation
    private Vector3 settledPosition;
    private bool hasSettled = false;

    /// <summary>
    /// Current state of this collectable
    /// </summary>
    public CollectableState State => state;

    /// <summary>
    /// Whether this item can currently be collected
    /// </summary>
    public bool CanBeCollected => state == CollectableState.Idle && Time.time > spawnTime + settleTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        originalScale = transform.localScale;
        spawnTime = Time.time;
    }

    private void Update()
    {
        switch (state)
        {
            case CollectableState.Idle:
                UpdateIdle();
                break;

            case CollectableState.BeingCollected:
                UpdateCollection();
                break;
        }
    }

    private void UpdateIdle()
    {
        // Check if physics has settled
        if (!hasSettled && rb.IsSleeping())
        {
            hasSettled = true;
            settledPosition = transform.position;
            rb.isKinematic = true;
        }

        // Gentle bob and rotate when settled
        if (hasSettled)
        {
            float bob = Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
            transform.position = settledPosition + Vector3.up * bob;
            transform.Rotate(Vector3.up, idleRotateSpeed * Time.deltaTime);
        }
    }

    private void UpdateCollection()
    {
        if (collectTarget == null)
        {
            // Target lost, return to idle
            CancelCollection();
            return;
        }

        float elapsed = Time.time - collectStartTime;
        float t = Mathf.Clamp01(elapsed / collectDuration);

        // Lerp toward collector with easing
        Vector3 targetPos = collectTarget.position;
        transform.position = Vector3.Lerp(collectStartPos, targetPos, EaseInOutQuad(t));

        // Shrink as approaching end
        float shrinkT = Mathf.Clamp01((elapsed - (collectDuration - shrinkDuration)) / shrinkDuration);
        transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, shrinkT);

        // Check if reached destination
        if (t >= 1f)
        {
            CompleteCollection();
        }
    }

    private float EaseInOutQuad(float t)
    {
        return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
    }

    /// <summary>
    /// Start being collected by a Collector
    /// </summary>
    public void StartCollection(Transform collector)
    {
        if (state != CollectableState.Idle) return;

        state = CollectableState.BeingCollected;
        collectTarget = collector;
        collectStartTime = Time.time;
        collectStartPos = transform.position;

        // Disable physics and collision
        rb.isKinematic = true;
        col.enabled = false;
    }

    private void CompleteCollection()
    {
        state = CollectableState.Collected;
        Destroy(gameObject);
    }

    /// <summary>
    /// Cancel collection (e.g., if collector is dropped)
    /// </summary>
    public void CancelCollection()
    {
        if (state != CollectableState.BeingCollected) return;

        state = CollectableState.Idle;
        collectTarget = null;
        transform.localScale = originalScale;

        // Re-enable physics
        rb.isKinematic = false;
        col.enabled = true;
        hasSettled = false;
    }

    /// <summary>
    /// Force collection immediately (skip animation)
    /// </summary>
    public void CollectImmediate()
    {
        state = CollectableState.Collected;
        Destroy(gameObject);
    }
}
