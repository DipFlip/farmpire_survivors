using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Abstract base class for holdable items that deal damage through direct collision.
/// Unlike ProjectileHoldableItem, does NOT fire projectiles.
/// Damage is dealt via OnTriggerEnter when the item's collider hits a target.
/// Orbits continuously around the player at a set speed.
///
/// Setup:
/// - Add a Collider component with isTrigger = true
/// - Add a Rigidbody component (isKinematic = true) - REQUIRED for OnTriggerEnter
/// - Targets should have regular colliders (not triggers)
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public abstract class MeleeHoldableItem : HoldableItemBase
{
    [Header("Melee Stats")]
    [Tooltip("Damage dealt per hit")]
    [SerializeField] protected float damageAmount = 10f;

    [Tooltip("Cooldown between hits on the same target")]
    [SerializeField] protected float hitCooldown = 0.5f;

    [Header("Orbit")]
    [Tooltip("Orbit speed in degrees per second")]
    [SerializeField] protected float orbitDegreesPerSecond = 180f;

    [Header("Collider")]
    [Tooltip("Trigger collider used for hit detection (assign or will auto-find)")]
    [SerializeField] protected Collider meleeCollider;

    // Track hit cooldowns per target to prevent rapid hitting same target
    protected Dictionary<Collider, float> hitCooldowns = new Dictionary<Collider, float>();

    protected override void Awake()
    {
        base.Awake();

        // Auto-find trigger collider if not assigned
        if (meleeCollider == null)
        {
            meleeCollider = GetComponent<Collider>();
        }

        // Ensure collider is a trigger
        if (meleeCollider != null && !meleeCollider.isTrigger)
        {
            Debug.LogWarning($"{GetType().Name} '{name}': Collider should be a trigger for melee detection. Setting isTrigger = true.");
            meleeCollider.isTrigger = true;
        }

        // Setup Rigidbody for trigger detection (required for OnTriggerEnter)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    protected override void Update()
    {
        if (currentState != ItemState.Equipped || holder == null)
        {
            CleanupCooldowns();
            return;
        }

        // Continuous orbit - just increment angle at constant speed
        orbitAngle += orbitDegreesPerSecond * Time.deltaTime;
        if (orbitAngle > 360f) orbitAngle -= 360f;

        // Position around holder
        float rad = orbitAngle * Mathf.Deg2Rad;
        Vector3 orbitOffset = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * orbitRadius;
        orbitOffset.y = orbitHeight;

        Vector3 targetPos = holder.position + orbitOffset;
        transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);

        // Face the direction of movement (tangent to orbit)
        Vector3 tangent = new Vector3(Mathf.Cos(rad), 0f, -Mathf.Sin(rad));
        if (tangent.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(tangent);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        CleanupCooldowns();
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[{GetType().Name}] OnTriggerEnter: {other.name} (tag: {other.tag})");

        // Only deal damage when equipped
        if (currentState != ItemState.Equipped)
        {
            Debug.Log($"[{GetType().Name}] Not equipped, ignoring");
            return;
        }

        // Check if this is a valid target by tag
        if (!other.CompareTag(TargetTag))
        {
            Debug.Log($"[{GetType().Name}] Tag mismatch: expected '{TargetTag}', got '{other.tag}'");
            return;
        }

        // Validate target (subclass-specific logic)
        if (!IsValidTarget(other.gameObject))
        {
            Debug.Log($"[{GetType().Name}] IsValidTarget returned false for {other.name}");
            return;
        }

        // Check cooldown for this specific target
        if (hitCooldowns.TryGetValue(other, out float lastHitTime))
        {
            if (Time.time < lastHitTime + hitCooldown)
            {
                Debug.Log($"[{GetType().Name}] On cooldown for {other.name}");
                return;
            }
        }

        // Deal damage
        Debug.Log($"[{GetType().Name}] Dealing {damageAmount} damage to {other.name}");
        DealDamage(other.gameObject, damageAmount);
        hitCooldowns[other] = Time.time;

        // Visual and audio feedback
        PlayPulse();
        PlayActionSound();
    }

    /// <summary>
    /// Deal damage to the target. Subclasses implement specific damage logic.
    /// </summary>
    protected abstract void DealDamage(GameObject targetObj, float amount);

    /// <summary>
    /// Remove cooldown entries for destroyed/null colliders
    /// </summary>
    private void CleanupCooldowns()
    {
        var toRemove = hitCooldowns.Keys.Where(k => k == null).ToList();
        foreach (var key in toRemove)
        {
            hitCooldowns.Remove(key);
        }
    }

    public override void Drop(Vector3 position)
    {
        base.Drop(position);
        hitCooldowns.Clear();
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        // Draw melee collider bounds
        if (meleeCollider != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.DrawWireCube(meleeCollider.bounds.center, meleeCollider.bounds.size);
        }
    }
}
