using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

/// <summary>
/// A station where players can deposit collected items from a Collector.
/// Can require specific item types and amounts.
/// Triggers events when requirements are met and items are deposited.
///
/// Setup:
/// - Configure detection range
/// - Configure required items in Inspector
/// - Hook up OnDepositComplete event for what happens after deposit
/// </summary>
public class DepositStation : MonoBehaviour
{
    [Serializable]
    public class ItemRequirement
    {
        [Tooltip("Item type identifier (must match CollectableItem.itemType)")]
        public string itemType;

        [Tooltip("Amount required")]
        public int amount = 1;

        [HideInInspector]
        public int currentAmount = 0;
    }

    [Header("Requirements")]
    [Tooltip("Items required to complete this deposit")]
    [SerializeField] private List<ItemRequirement> requirements = new List<ItemRequirement>();

    [Header("Behavior")]
    [Tooltip("Can be used multiple times (resets after completion)")]
    [SerializeField] private bool repeatable = false;

    [Header("Spawn On Complete")]
    [Tooltip("Prefab to spawn when deposit is complete")]
    [SerializeField] private GameObject spawnOnCompletePrefab;

    [Tooltip("Offset from station position to spawn the prefab")]
    [SerializeField] private Vector3 spawnOffset = Vector3.zero;

    [Tooltip("Use station's rotation for spawned object")]
    [SerializeField] private bool useStationRotation = true;

    [Header("Destruction")]
    [Tooltip("Destroy this station after completion")]
    [SerializeField] private bool destroyOnComplete = false;

    [Tooltip("Delay before destroying (allows effects to play)")]
    [SerializeField] private float destroyDelay = 0.5f;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject incompleteVisual;
    [SerializeField] private GameObject completeVisual;
    [SerializeField] private float pulseScale = 1.1f;
    [SerializeField] private float pulseDuration = 0.3f;

    [Header("Effects")]
    [SerializeField] private GameObject depositEffectPrefab;
    [SerializeField] private GameObject completeEffectPrefab;

    [Header("Sound")]
    [SerializeField] private AudioClip depositSound;
    [SerializeField] private AudioClip completeSound;
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.1f;

    [Header("Events")]
    [Tooltip("Called each time items are deposited")]
    public UnityEvent OnDeposit;

    [Tooltip("Called when all requirements are met")]
    public UnityEvent OnDepositComplete;

    private bool isComplete = false;

    /// <summary>
    /// Whether all requirements have been met
    /// </summary>
    public bool IsComplete => isComplete;

    /// <summary>
    /// Overall progress from 0 to 1
    /// </summary>
    public float Progress
    {
        get
        {
            if (requirements.Count == 0) return 1f;

            int totalRequired = 0;
            int totalCurrent = 0;

            foreach (var req in requirements)
            {
                totalRequired += req.amount;
                totalCurrent += req.currentAmount;
            }

            return totalRequired > 0 ? (float)totalCurrent / totalRequired : 1f;
        }
    }

    /// <summary>
    /// Get current/required for a specific item type
    /// </summary>
    public (int current, int required) GetRequirementStatus(string itemType)
    {
        foreach (var req in requirements)
        {
            if (req.itemType == itemType)
            {
                return (req.currentAmount, req.amount);
            }
        }
        return (0, 0);
    }

    /// <summary>
    /// Get all requirements with their current status
    /// </summary>
    public IReadOnlyList<ItemRequirement> Requirements => requirements;

    private void Start()
    {
        UpdateVisuals();
    }

    /// <summary>
    /// Receive an item from a Collector. Called by CollectorHoldableItem.
    /// </summary>
    public bool ReceiveItem(string itemType, int amount)
    {
        if (isComplete && !repeatable) return false;

        // Find matching requirement
        foreach (var req in requirements)
        {
            if (req.itemType != itemType) continue;
            if (req.currentAmount >= req.amount) continue;

            int needed = req.amount - req.currentAmount;
            int toAdd = Mathf.Min(needed, amount);

            req.currentAmount += toAdd;
            Debug.Log($"[DepositStation] Received {toAdd}x {itemType} ({req.currentAmount}/{req.amount})");

            PlayDepositFeedback();
            OnDeposit?.Invoke();

            if (CheckComplete())
            {
                CompleteDeposit();
            }

            return true;
        }

        return false; // Item type not needed
    }

    /// <summary>
    /// Check if this station needs a specific item type
    /// </summary>
    public bool NeedsItem(string itemType)
    {
        foreach (var req in requirements)
        {
            if (req.itemType == itemType && req.currentAmount < req.amount)
            {
                return true;
            }
        }
        return false;
    }

    private bool CheckComplete()
    {
        foreach (var req in requirements)
        {
            if (req.currentAmount < req.amount)
            {
                return false;
            }
        }
        return true;
    }

    private void CompleteDeposit()
    {
        isComplete = true;
        Debug.Log($"[DepositStation] {name} complete!");

        PlayCompleteFeedback();
        UpdateVisuals();
        SpawnCompletePrefab();
        OnDepositComplete?.Invoke();

        if (repeatable)
        {
            // Reset for next use
            ResetStation();
        }
        else if (destroyOnComplete)
        {
            Destroy(gameObject, destroyDelay);
        }
    }

    private void SpawnCompletePrefab()
    {
        if (spawnOnCompletePrefab == null) return;

        Vector3 spawnPos = transform.position + spawnOffset;
        Quaternion spawnRot = useStationRotation ? transform.rotation : Quaternion.identity;

        GameObject spawned = Instantiate(spawnOnCompletePrefab, spawnPos, spawnRot);
        Debug.Log($"[DepositStation] Spawned {spawnOnCompletePrefab.name} at {spawnPos}");
    }

    private void PlayDepositFeedback()
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
        if (depositEffectPrefab != null)
        {
            SpawnEffect(depositEffectPrefab);
        }

        // Sound
        if (depositSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound(depositSound, minPitch, maxPitch);
        }

        UpdateVisuals();
    }

    private void PlayCompleteFeedback()
    {
        // Effect
        if (completeEffectPrefab != null)
        {
            SpawnEffect(completeEffectPrefab);
        }

        // Sound
        if (completeSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound(completeSound, minPitch, maxPitch);
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

    private void UpdateVisuals()
    {
        if (incompleteVisual != null)
        {
            incompleteVisual.SetActive(!isComplete);
        }

        if (completeVisual != null)
        {
            completeVisual.SetActive(isComplete);
        }
    }

    /// <summary>
    /// Reset station to initial state
    /// </summary>
    public void ResetStation()
    {
        isComplete = false;

        foreach (var req in requirements)
        {
            req.currentAmount = 0;
        }

        UpdateVisuals();
    }

    private void OnDrawGizmosSelected()
    {
        // Draw requirements as text in scene view
        #if UNITY_EDITOR
        Vector3 labelPos = transform.position + Vector3.up * 2f;
        string label = "Requirements:\n";
        foreach (var req in requirements)
        {
            label += $"{req.itemType}: {req.currentAmount}/{req.amount}\n";
        }
        UnityEditor.Handles.Label(labelPos, label);
        #endif
    }
}
