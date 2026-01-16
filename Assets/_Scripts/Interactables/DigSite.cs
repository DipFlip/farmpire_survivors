using UnityEngine;
using DG.Tweening;

/// <summary>
/// A dig site that can be dug up with a shovel and then planted with seeds.
/// States: Undig -> Dug -> Planted (spawns plant)
/// </summary>
public class DigSite : MonoBehaviour, ITargetable
{
    public enum DigState { Undig, Dug, Planted }

    [Header("Visual Stages")]
    [Tooltip("Visual for undig state (dirt mound)")]
    [SerializeField] private GameObject undigVisual;
    [Tooltip("Visual for dug state (hole/tilled soil)")]
    [SerializeField] private GameObject dugVisual;
    [Tooltip("Optional visual for planted state")]
    [SerializeField] private GameObject plantedVisual;

    [Header("Dig Settings")]
    [Tooltip("Amount of digging needed to complete")]
    [SerializeField] private float digRequired = 30f;

    [Header("Seed Settings")]
    [Tooltip("Amount of seeds needed to spawn plant")]
    [SerializeField] private float seedsRequired = 20f;

    [Header("Plant Spawning")]
    [Tooltip("Plant prefab to spawn when fully seeded")]
    [SerializeField] private GameObject plantPrefab;
    [Tooltip("Offset from dig site center for plant spawn")]
    [SerializeField] private Vector3 plantSpawnOffset = Vector3.zero;

    [Header("Effects")]
    [SerializeField] private GameObject digCompleteEffectPrefab;
    [SerializeField] private GameObject plantSpawnEffectPrefab;

    [Header("Hit Pulse Animation")]
    [SerializeField] private float pulseDuration = 0.5f;
    [SerializeField] private float pulseCycleTime = 0.25f;
    [SerializeField] private float pulseScale = 1.1f;

    [Header("Current State")]
    [SerializeField] private DigState currentState = DigState.Undig;
    [SerializeField] private float currentDig = 0f;
    [SerializeField] private float currentSeeds = 0f;

    // Pulse state
    private Tween pulseTween;
    private float pulseEndTime;
    private Vector3 originalVisualScale;

    // ITargetable implementation
    public bool CanReceive => currentState == DigState.Undig || currentState == DigState.Dug;

    // Public accessors
    public DigState CurrentDigState => currentState;
    public bool CanDig => currentState == DigState.Undig;
    public bool CanPlant => currentState == DigState.Dug;
    public float DigProgress => Mathf.Clamp01(currentDig / digRequired);
    public float SeedProgress => Mathf.Clamp01(currentSeeds / seedsRequired);
    public float CurrentDig => currentDig;
    public float CurrentSeeds => currentSeeds;

    private void Start()
    {
        UpdateVisuals();
    }

    private void Update()
    {
        // Check if pulse should stop
        if (pulseTween != null && Time.time >= pulseEndTime)
        {
            StopPulse();
        }
    }

    // Generic receive from ITargetable - not used directly,
    // use ReceiveDig or ReceiveSeed instead
    public void ReceiveAmount(float amount)
    {
        // Route based on current state
        if (currentState == DigState.Undig)
            ReceiveDig(amount);
        else if (currentState == DigState.Dug)
            ReceiveSeed(amount);
    }

    /// <summary>
    /// Receive digging from a shovel projectile
    /// </summary>
    public void ReceiveDig(float amount)
    {
        if (!CanDig) return;

        currentDig += amount;
        StartOrExtendPulse(undigVisual);

        if (currentDig >= digRequired)
        {
            CompleteDig();
        }
    }

    /// <summary>
    /// Receive seeds from a seed bag projectile
    /// </summary>
    public void ReceiveSeed(float amount)
    {
        if (!CanPlant) return;

        currentSeeds += amount;
        StartOrExtendPulse(dugVisual);

        if (currentSeeds >= seedsRequired)
        {
            SpawnPlant();
        }
    }

    private void CompleteDig()
    {
        StopPulse();
        currentState = DigState.Dug;
        currentDig = 0f;
        UpdateVisuals();
        SpawnEffect(digCompleteEffectPrefab);
    }

    private void SpawnPlant()
    {
        StopPulse();
        currentState = DigState.Planted;
        currentSeeds = 0f;
        UpdateVisuals();
        SpawnEffect(plantSpawnEffectPrefab);

        if (plantPrefab != null)
        {
            Vector3 spawnPos = transform.position + plantSpawnOffset;
            GameObject plant = Instantiate(plantPrefab, spawnPos, Quaternion.identity);

            // Optional: scale animation for the spawned plant
            Transform plantTransform = plant.transform;
            Vector3 originalScale = plantTransform.localScale;
            plantTransform.localScale = originalScale * 0.1f;
            plantTransform.DOScale(originalScale, 0.35f).SetEase(Ease.OutBack, 1.5f);
        }
    }

    private void UpdateVisuals()
    {
        if (undigVisual != null) undigVisual.SetActive(currentState == DigState.Undig);
        if (dugVisual != null) dugVisual.SetActive(currentState == DigState.Dug);
        if (plantedVisual != null) plantedVisual.SetActive(currentState == DigState.Planted);
    }

    private void StartOrExtendPulse(GameObject visual)
    {
        if (visual == null) return;

        pulseEndTime = Time.time + pulseDuration;

        // If already pulsing, just extend timer
        if (pulseTween != null) return;

        Transform t = visual.transform;
        originalVisualScale = t.localScale;

        // Start from original scale
        t.localScale = originalVisualScale;

        // Create looping pulse
        pulseTween = t.DOScale(originalVisualScale * pulseScale, pulseCycleTime / 2f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StopPulse()
    {
        if (pulseTween == null) return;

        pulseTween.Kill();
        pulseTween = null;
    }

    private void SpawnEffect(GameObject effectPrefab)
    {
        if (effectPrefab == null) return;

        GameObject effect = Instantiate(effectPrefab, transform.position, Quaternion.identity);

        if (effect.TryGetComponent<ParticleSystem>(out var ps))
        {
            Destroy(effect, ps.main.duration + ps.main.startLifetime.constantMax);
        }
        else
        {
            Destroy(effect, 3f);
        }
    }

    /// <summary>
    /// Reset dig site to initial undig state
    /// </summary>
    public void ResetDigSite()
    {
        StopPulse();
        currentState = DigState.Undig;
        currentDig = 0f;
        currentSeeds = 0f;
        UpdateVisuals();
    }

    private void OnDrawGizmosSelected()
    {
        // Draw plant spawn position
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position + plantSpawnOffset, 0.3f);
    }
}
