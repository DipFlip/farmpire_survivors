using UnityEngine;
using DG.Tweening;

/// <summary>
/// A station that refills ProjectileHoldableItems when a player enters its trigger.
/// Matches items by resourceType string.
///
/// Setup:
/// - Add a trigger collider to this GameObject or a child
/// - Set resourceType to match the item's resourceType (e.g., "Water", "Seeds")
/// - Configure refill amount and rate
/// </summary>
public class ProjectileRefill : MonoBehaviour
{
    [Header("Resource")]
    [Tooltip("Must match the item's resourceType to refill")]
    [SerializeField] private ResourceType resourceType = ResourceType.Water;

    [Tooltip("Amount to refill per tick. -1 = instant full refill")]
    [SerializeField] private float refillAmount = -1f;

    [Header("Visual Feedback")]
    [SerializeField] private float pulseScale = 1.1f;
    [SerializeField] private float pulseDuration = 0.3f;

    [Header("Effects")]
    [SerializeField] private GameObject refillEffectPrefab;
    [SerializeField] private float refillEffectScale = 1f;
    [Tooltip("Where to spawn the effect. If empty, uses this transform's position")]
    [SerializeField] private Transform refillEffectPosition;

    [Header("Sound")]
    [SerializeField] private AudioClip refillSound;
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.1f;

    [Header("Hover")]
    [Tooltip("Height above the station the item floats while refilling")]
    [SerializeField] private float hoverHeight = 1.5f;

    private ItemHolder currentHolder;
    private ProjectileHoldableItem currentItem;

    public ResourceType ResourceType => resourceType;
    public float RefillAmount => refillAmount;

    /// <summary>
    /// Called by the item when it's full during a bob cycle
    /// </summary>
    public void StopFromItem()
    {
        StopRefilling();
    }

    private void OnTriggerEnter(Collider other)
    {
        ItemHolder holder = other.GetComponent<ItemHolder>();
        if (holder == null) holder = other.GetComponentInParent<ItemHolder>();
        if (holder == null) return;

        // Prevent multiple triggers from different colliders on same holder
        if (holder == currentHolder) return;

        ProjectileHoldableItem item = GetMatchingItem(holder);
        if (item == null) return;

        currentHolder = holder;
        currentItem = item;

        if (item.CurrentResource >= item.MaxResource) return;

        if (refillAmount < 0)
        {
            // Instant full refill
            item.AddResource(item.MaxResource - item.CurrentResource);
            PlayRefillFeedback();
        }
        else
        {
            // Continuous refill driven by item's bob animation
            item.StartRefillHover(this, hoverHeight);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        ItemHolder holder = other.GetComponent<ItemHolder>();
        if (holder == null) holder = other.GetComponentInParent<ItemHolder>();

        if (holder == currentHolder)
        {
            StopRefilling();
        }
    }

    private void StopRefilling()
    {
        if (currentItem != null)
        {
            currentItem.StopRefillHover();
        }
        currentItem = null;
        currentHolder = null;
    }

    private ProjectileHoldableItem GetMatchingItem(ItemHolder holder)
    {
        if (holder.EquippedItem == null) return null;

        ProjectileHoldableItem item = holder.EquippedItem as ProjectileHoldableItem;
        if (item == null) return null;

        // Check if resourceType matches
        if (resourceType != ResourceType.None && item.ResourceType != resourceType)
        {
            return null;
        }

        // Only refill items that have limited resources
        if (!item.HasLimitedResource) return null;

        return item;
    }

    public void PlayRefillFeedback()
    {
        // Pulse animation
        transform.DOScale(transform.localScale * pulseScale, pulseDuration / 2f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                transform.DOScale(transform.localScale / pulseScale, pulseDuration / 2f)
                    .SetEase(Ease.InQuad);
            });

        // Effect
        if (refillEffectPrefab != null)
        {
            SpawnEffect(refillEffectPrefab);
        }

        PlaySound();
    }

    private void PlaySound()
    {
        if (refillSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound(refillSound, minPitch, maxPitch);
        }
    }

    private void SpawnEffect(GameObject prefab)
    {
        Vector3 spawnPos = refillEffectPosition != null ? refillEffectPosition.position : transform.position;
        GameObject effect = Instantiate(prefab, spawnPos, Quaternion.identity);
        effect.transform.localScale = Vector3.one * refillEffectScale;

        if (effect.TryGetComponent<ParticleSystem>(out var ps))
        {
            Destroy(effect, ps.main.duration + ps.main.startLifetime.constantMax);
        }
        else
        {
            Destroy(effect, 3f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.5f);

        // Draw resource type label
        #if UNITY_EDITOR
        Vector3 labelPos = transform.position + Vector3.up * 1.5f;
        UnityEditor.Handles.Label(labelPos, $"Refill: {resourceType}");
        #endif
    }
}
