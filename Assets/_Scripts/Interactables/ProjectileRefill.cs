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

    [Tooltip("Time between refill ticks (only used if refillAmount > 0)")]
    [SerializeField] private float refillRate = 0.1f;

    [Header("Visual Feedback")]
    [SerializeField] private float pulseScale = 1.1f;
    [SerializeField] private float pulseDuration = 0.3f;

    [Header("Effects")]
    [SerializeField] private GameObject refillEffectPrefab;

    [Header("Sound")]
    [SerializeField] private AudioClip refillSound;
    [Tooltip("Time between sound plays during gradual refill. 0 = play every tick")]
    [SerializeField] private float soundRate = 0.2f;
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.1f;

    private float lastRefillTime;
    private float lastSoundTime;
    private ItemHolder currentHolder;
    private bool isRefilling;

    public ResourceType ResourceType => resourceType;

    private void Update()
    {
        if (!isRefilling || currentHolder == null) return;

        ProjectileHoldableItem item = GetMatchingItem(currentHolder);
        if (item == null)
        {
            isRefilling = false;
            return;
        }

        // Continuous refill
        if (refillAmount > 0 && Time.time >= lastRefillTime + refillRate)
        {
            if (item.CurrentResource < item.MaxResource)
            {
                item.AddResource(refillAmount);
                lastRefillTime = Time.time;

                // Play sound at soundRate interval
                if (Time.time >= lastSoundTime + soundRate)
                {
                    PlaySound();
                    lastSoundTime = Time.time;
                }
            }
            else
            {
                isRefilling = false;
            }
        }
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

        if (refillAmount < 0)
        {
            // Instant full refill
            float amountToAdd = item.MaxResource - item.CurrentResource;
            if (amountToAdd > 0)
            {
                item.AddResource(amountToAdd);
                PlayRefillFeedback();
            }
        }
        else
        {
            // Start continuous refill
            isRefilling = true;
            lastRefillTime = Time.time - refillRate; // Allow immediate first tick
            PlayRefillFeedback();
            lastSoundTime = Time.time; // Sound already played in feedback, wait for next interval
        }
    }

    private void OnTriggerExit(Collider other)
    {
        ItemHolder holder = other.GetComponent<ItemHolder>();
        if (holder == null) holder = other.GetComponentInParent<ItemHolder>();

        if (holder == currentHolder)
        {
            isRefilling = false;
            currentHolder = null;
        }
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

    private void PlayRefillFeedback()
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
        GameObject effect = Instantiate(prefab, transform.position, Quaternion.identity);

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
