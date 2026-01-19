using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Manages plant growth through watering. Growth stages are child GameObjects
/// that get enabled/disabled as the plant levels up.
/// At max level, spawns harvestable CollectableItems. When all are collected,
/// the plant drops back to a lower level and can regrow.
///
/// Setup:
/// - Parent: This script
/// - Children: Each growth level prefab with mesh + BoxCollider + "Plant" tag
/// - Assign children to growthStages array in order
/// - Optionally assign harvestPrefab for collectables at max level
/// </summary>
public class Plant : MonoBehaviour, ITargetable
{
    // ITargetable implementation
    public bool CanReceive => CanGrow;
    public void ReceiveAmount(float amount) => ReceiveWater(amount);

    [Header("Growth Stages")]
    [Tooltip("Child GameObjects for each growth level (assign in order: Lv1, Lv2, Lv3, etc.)")]
    [SerializeField] private GameObject[] growthStages;

    [Header("Growth Settings")]
    [Tooltip("Amount of water needed to advance to the next level")]
    [SerializeField] private float waterPerLevel = 50f;

    [Header("Effects")]
    [Tooltip("Particle effect prefab to spawn on level up (should auto-destroy or have Stop Action: Destroy)")]
    [SerializeField] private GameObject levelUpEffectPrefab;

    [Header("Sound")]
    [SerializeField] private AudioClip levelUpSound;
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.1f;

    [Header("Level Up Animation")]
    [SerializeField] private float scaleAnimationDuration = 0.35f;
    [SerializeField] private float startScale = 0.5f;
    [SerializeField] private float overshoot = 1.5f; // Higher = more bounce

    [Header("Water Hit Pulse")]
    [SerializeField] private float pulseDuration = 0.5f;
    [SerializeField] private float pulseCycleTime = 0.25f; // Time for one up-down cycle
    [SerializeField] private float pulseScale = 1.1f; // How much bigger during pulse

    [Header("Harvest")]
    [Tooltip("Prefab for max level stage (will be instantiated fresh each time). Leave empty to use growthStages array.")]
    [SerializeField] private GameObject maxLevelPrefab;

    [Tooltip("Level to drop to after harvest is collected (1-indexed). Set to 0 to disable harvest.")]
    [SerializeField] private int levelAfterHarvest = 2;

    [Header("Current State")]
    [Tooltip("Current growth level (1 = first stage)")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private float currentWater = 0f;

    // Pulse state
    private Tween pulseTween;
    private float pulseEndTime;
    private Vector3 originalStageScale;

    // Harvest tracking
    private GameObject spawnedMaxLevelInstance;
    private List<CollectableItem> harvestItems = new List<CollectableItem>();
    private bool watchingHarvest = false;

    /// <summary>
    /// Current water amount toward next level
    /// </summary>
    public float CurrentWater => currentWater;

    /// <summary>
    /// Water needed per level
    /// </summary>
    public float WaterPerLevel => waterPerLevel;

    /// <summary>
    /// Current growth level (1-indexed)
    /// </summary>
    public int CurrentLevel => currentLevel;

    /// <summary>
    /// Maximum level (1-indexed)
    /// </summary>
    public int MaxLevel => growthStages != null ? growthStages.Length : 0;

    /// <summary>
    /// Progress from 0 to 1 toward next level
    /// </summary>
    public float Progress => Mathf.Clamp01(currentWater / waterPerLevel);

    /// <summary>
    /// Whether this plant can still grow
    /// </summary>
    public bool CanGrow => currentLevel < MaxLevel && !watchingHarvest;

    /// <summary>
    /// Whether this plant has active harvest waiting to be collected
    /// </summary>
    public bool HasActiveHarvest => watchingHarvest;

    // Updates visibility in editor when you change values
    private void OnValidate()
    {
#if UNITY_EDITOR
        // Delay to avoid "SendMessage cannot be called during OnValidate" warning
        EditorApplication.delayCall += UpdateVisibleStage;
#endif
    }

    private void Start()
    {
        UpdateVisibleStage();

        // If starting at max level with a prefab, spawn it
        if (currentLevel >= MaxLevel && maxLevelPrefab != null && levelAfterHarvest > 0)
        {
            SpawnMaxLevelAndWatch();
        }
    }

    private void UpdateVisibleStage()
    {
        if (this == null) return; // Object may have been destroyed before delayed call
        if (growthStages == null || growthStages.Length == 0) return;

        // Clamp current level to valid range
        currentLevel = Mathf.Clamp(currentLevel, 1, growthStages.Length);

        // Disable all stages, then enable current (convert 1-indexed to array index)
        for (int i = 0; i < growthStages.Length; i++)
        {
            if (growthStages[i] != null)
            {
                growthStages[i].SetActive(i == currentLevel - 1);
            }
        }
    }

    private void Update()
    {
        // Check if pulse should stop
        if (pulseTween != null && Time.time >= pulseEndTime)
        {
            StopPulse();
        }

        // Check if harvest has been collected
        if (watchingHarvest)
        {
            CheckHarvestCollected();
        }
    }

    /// <summary>
    /// Add water to this plant. If threshold is reached, evolves to next level.
    /// </summary>
    public void ReceiveWater(float amount)
    {
        if (!CanGrow) return;

        currentWater += amount;

        // Start or extend pulse
        StartOrExtendPulse();

        if (currentWater >= waterPerLevel)
        {
            LevelUp();
        }
    }

    private void StartOrExtendPulse()
    {
        // Always reset the timer
        pulseEndTime = Time.time + pulseDuration;

        // If already pulsing, just extend timer
        if (pulseTween != null) return;

        // Get current stage
        int currentIndex = currentLevel - 1;
        if (currentIndex < 0 || currentIndex >= growthStages.Length) return;
        GameObject currentStage = growthStages[currentIndex];
        if (currentStage == null) return;

        Transform t = currentStage.transform;
        originalStageScale = t.localScale;

        // Always start from original scale
        t.localScale = originalStageScale;

        // Create looping pulse: scale up then down, repeat
        pulseTween = t.DOScale(originalStageScale * pulseScale, pulseCycleTime / 2f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StopPulse()
    {
        if (pulseTween == null) return;

        pulseTween.Kill();
        pulseTween = null;
    }

    private void LevelUp()
    {
        if (!CanGrow) return;

        // Stop any active pulse before switching stages
        StopPulse();

        // Disable current stage (convert 1-indexed to array index)
        int currentIndex = currentLevel - 1;
        if (growthStages[currentIndex] != null)
        {
            growthStages[currentIndex].SetActive(false);
        }

        // Advance level
        currentLevel++;
        currentWater = 0f;

        // Check if we're reaching max level with a prefab
        bool useMaxLevelPrefab = currentLevel >= MaxLevel && maxLevelPrefab != null && levelAfterHarvest > 0;

        // Enable new stage with scale animation (skip if using prefab for max level)
        if (!useMaxLevelPrefab)
        {
            int newIndex = currentLevel - 1;
            if (newIndex >= 0 && newIndex < growthStages.Length && growthStages[newIndex] != null)
            {
                GameObject newStage = growthStages[newIndex];
                Transform t = newStage.transform;

                // Store original scale and start small
                Vector3 originalScale = t.localScale;
                t.localScale = originalScale * startScale;

                newStage.SetActive(true);

                // Animate to original scale with overshoot
                t.DOScale(originalScale, scaleAnimationDuration)
                    .SetEase(Ease.OutBack, overshoot);
            }
        }

        // Spawn level up effect and sound
        SpawnLevelUpEffect();
        PlayLevelUpSound();

        // Check if we reached max level - start watching for harvest collection
        if (currentLevel >= MaxLevel && levelAfterHarvest > 0)
        {
            StartWatchingHarvest();
        }
    }

    private void SpawnMaxLevelAndWatch()
    {
        if (maxLevelPrefab == null) return;

        // Destroy old instance if exists
        if (spawnedMaxLevelInstance != null)
        {
            Destroy(spawnedMaxLevelInstance);
        }

        // Disable the regular max level stage if it exists in growthStages
        int maxIndex = MaxLevel - 1;
        if (maxIndex >= 0 && maxIndex < growthStages.Length && growthStages[maxIndex] != null)
        {
            growthStages[maxIndex].SetActive(false);
        }

        // Spawn the prefab as a child
        spawnedMaxLevelInstance = Instantiate(maxLevelPrefab, transform);
        spawnedMaxLevelInstance.transform.localPosition = Vector3.zero;
        spawnedMaxLevelInstance.transform.localRotation = Quaternion.identity;

        // Find all CollectableItems in the spawned instance
        harvestItems.Clear();
        CollectableItem[] items = spawnedMaxLevelInstance.GetComponentsInChildren<CollectableItem>();
        foreach (var item in items)
        {
            harvestItems.Add(item);
        }

        if (harvestItems.Count > 0)
        {
            watchingHarvest = true;
            Debug.Log($"[Plant] {name} spawned max level prefab with {harvestItems.Count} harvest items");
        }
        else
        {
            Debug.LogWarning($"[Plant] {name} spawned max level prefab but found no CollectableItems!");
        }
    }

    private void StartWatchingHarvest()
    {
        // If we have a prefab, spawn it fresh
        if (maxLevelPrefab != null)
        {
            SpawnMaxLevelAndWatch();
            return;
        }

        // Legacy: use existing items in growthStages
        if (harvestItems.Count == 0) return;

        // Reactivate and reset all harvest items
        foreach (var item in harvestItems)
        {
            if (item != null)
            {
                item.ResetForHarvest();
            }
        }

        watchingHarvest = true;
        Debug.Log($"[Plant] {name} activated {harvestItems.Count} harvest items");
    }

    private void CheckHarvestCollected()
    {
        // Count active (not collected) harvest items
        int activeCount = 0;
        foreach (var item in harvestItems)
        {
            if (item != null && item.gameObject.activeSelf && item.State != CollectableItem.CollectableState.Collected)
            {
                activeCount++;
            }
        }

        // If all collected, drop to lower level
        if (activeCount == 0)
        {
            OnHarvestCollected();
        }
    }

    private void OnHarvestCollected()
    {
        watchingHarvest = false;

        Debug.Log($"[Plant] {name} harvest collected, dropping to level {levelAfterHarvest}");

        // Destroy spawned max level instance if exists
        if (spawnedMaxLevelInstance != null)
        {
            Destroy(spawnedMaxLevelInstance);
            spawnedMaxLevelInstance = null;
        }

        // Clear harvest items list
        harvestItems.Clear();

        // Drop to lower level
        SetLevel(levelAfterHarvest);
    }

    /// <summary>
    /// Set the plant to a specific level
    /// </summary>
    public void SetLevel(int newLevel)
    {
        if (growthStages == null || growthStages.Length == 0) return;

        StopPulse();

        // Disable current stage
        int currentIndex = currentLevel - 1;
        if (currentIndex >= 0 && currentIndex < growthStages.Length && growthStages[currentIndex] != null)
        {
            growthStages[currentIndex].SetActive(false);
        }

        // Set new level
        currentLevel = Mathf.Clamp(newLevel, 1, MaxLevel);
        currentWater = 0f;

        // Enable new stage with animation
        int newIndex = currentLevel - 1;
        if (newIndex >= 0 && newIndex < growthStages.Length && growthStages[newIndex] != null)
        {
            GameObject newStage = growthStages[newIndex];
            Transform t = newStage.transform;

            Vector3 originalScale = t.localScale;
            t.localScale = originalScale * startScale;
            newStage.SetActive(true);

            t.DOScale(originalScale, scaleAnimationDuration)
                .SetEase(Ease.OutBack, overshoot);
        }
    }

    private void PlayLevelUpSound()
    {
        if (levelUpSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound(levelUpSound, minPitch, maxPitch);
        }
    }

    private void SpawnLevelUpEffect()
    {
        if (levelUpEffectPrefab == null) return;

        GameObject effect = Instantiate(levelUpEffectPrefab, transform.position, Quaternion.identity);

        // Auto-destroy after particle system finishes
        if (effect.TryGetComponent<ParticleSystem>(out var ps))
        {
            Destroy(effect, ps.main.duration + ps.main.startLifetime.constantMax);
        }
        else
        {
            // Fallback: destroy after 3 seconds if no particle system found
            Destroy(effect, 3f);
        }
    }

    /// <summary>
    /// Reset plant to level 1 with zero water
    /// </summary>
    public void ResetPlant()
    {
        if (growthStages == null || growthStages.Length == 0) return;

        // Disable current
        int currentIndex = currentLevel - 1;
        if (currentIndex >= 0 && currentIndex < growthStages.Length && growthStages[currentIndex] != null)
        {
            growthStages[currentIndex].SetActive(false);
        }

        currentLevel = 1;
        currentWater = 0f;

        // Enable level 1
        if (growthStages[0] != null)
        {
            growthStages[0].SetActive(true);
        }
    }
}
