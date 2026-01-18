using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Abstract base class for holdable items that deal damage through direct collision.
/// Unlike ProjectileHoldableItem, does NOT fire projectiles.
/// Damage is dealt via OnTriggerEnter when the item's collider hits a target.
///
/// Setup:
/// - Add a Collider component with isTrigger = true
/// - Targets should have regular colliders (not triggers)
/// </summary>
public abstract class MeleeHoldableItem : HoldableItemBase
{
    [Header("Melee Stats")]
    [Tooltip("Damage dealt per hit")]
    [SerializeField] protected float damageAmount = 10f;

    [Tooltip("Cooldown between hits on the same target")]
    [SerializeField] protected float hitCooldown = 0.5f;

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
    }

    protected override void Update()
    {
        base.Update();
        CleanupCooldowns();
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        // Only deal damage when equipped
        if (currentState != ItemState.Equipped) return;

        // Check if this is a valid target by tag
        if (!other.CompareTag(TargetTag)) return;

        // Validate target (subclass-specific logic)
        if (!IsValidTarget(other.gameObject)) return;

        // Check cooldown for this specific target
        if (hitCooldowns.TryGetValue(other, out float lastHitTime))
        {
            if (Time.time < lastHitTime + hitCooldown) return;
        }

        // Deal damage
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
