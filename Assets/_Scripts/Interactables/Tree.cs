using UnityEngine;
using DG.Tweening;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// A tree that can be chopped down with an axe.
/// When fully chopped, falls and spawns CollectableItem logs.
///
/// Setup:
/// - Parent: This script with BoxCollider + "Tree" tag
/// - Children: Visual stages for damage levels (healthy -> damaged)
/// - Assign children to damageStages array in order
/// - Assign stump visual (initially disabled)
/// - Assign log prefab (CollectableItem)
/// </summary>
public class Tree : MonoBehaviour, ITargetable
{
    // ITargetable implementation
    public bool CanReceive => CanChop;
    public void ReceiveAmount(float amount) => ReceiveChop(amount);

    [Header("Damage Stages")]
    [Tooltip("Child GameObjects for each damage level (assign in order: healthy, slightly damaged, very damaged)")]
    [SerializeField] private GameObject[] damageStages;

    [Tooltip("Stump visual shown after tree falls (initially disabled)")]
    [SerializeField] private GameObject stumpVisual;

    [Header("Chop Settings")]
    [Tooltip("Total chop damage needed to fell the tree")]
    [SerializeField] private float chopRequired = 100f;

    [Header("Log Spawning")]
    [Tooltip("CollectableItem prefab to spawn when tree falls")]
    [SerializeField] private GameObject logPrefab;

    [Tooltip("Number of logs to spawn")]
    [SerializeField] private int logsToSpawn = 3;

    [Tooltip("Force applied to scatter logs outward")]
    [SerializeField] private float logScatterForce = 5f;

    [Tooltip("Upward force applied to logs")]
    [SerializeField] private float logScatterUpForce = 3f;

    [Tooltip("Offset from tree position to spawn logs")]
    [SerializeField] private Vector3 logSpawnOffset = Vector3.up;

    [Header("Fall Animation")]
    [SerializeField] private float fallDuration = 1.5f;
    [SerializeField] private float fallAngle = 90f;

    [Header("Effects")]
    [SerializeField] private GameObject chopEffectPrefab;
    [SerializeField] private GameObject fallEffectPrefab;

    [Header("Sound")]
    [SerializeField] private AudioClip chopSound;
    [SerializeField] private AudioClip fallSound;
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.1f;

    [Header("Hit Pulse Animation")]
    [SerializeField] private float pulseDuration = 0.3f;
    [SerializeField] private float pulseCycleTime = 0.15f;
    [SerializeField] private float pulseScale = 1.05f;

    [Header("Current State")]
    [SerializeField] private float currentChop = 0f;
    [SerializeField] private bool isFallen = false;

    // Pulse state
    private Tween pulseTween;
    private float pulseEndTime;
    private Vector3 originalStageScale;

    /// <summary>
    /// Current chop damage accumulated
    /// </summary>
    public float CurrentChop => currentChop;

    /// <summary>
    /// Total chop needed to fell the tree
    /// </summary>
    public float ChopRequired => chopRequired;

    /// <summary>
    /// Remaining health before tree falls
    /// </summary>
    public float ChopHealth => Mathf.Max(0f, chopRequired - currentChop);

    /// <summary>
    /// Progress from 0 to 1 toward felling
    /// </summary>
    public float ChopProgress => Mathf.Clamp01(currentChop / chopRequired);

    /// <summary>
    /// Whether this tree can still be chopped
    /// </summary>
    public bool CanChop => !isFallen;

    /// <summary>
    /// Whether the tree has fallen
    /// </summary>
    public bool IsFallen => isFallen;

    private void OnValidate()
    {
#if UNITY_EDITOR
        EditorApplication.delayCall += UpdateVisibleStage;
#endif
    }

    private void Start()
    {
        UpdateVisibleStage();

        if (stumpVisual != null)
        {
            stumpVisual.SetActive(false);
        }
    }

    private void Update()
    {
        if (pulseTween != null && Time.time >= pulseEndTime)
        {
            StopPulse();
        }
    }

    private void UpdateVisibleStage()
    {
        if (this == null) return;
        if (damageStages == null || damageStages.Length == 0) return;
        if (isFallen) return;

        // Calculate which damage stage to show based on progress
        float progress = ChopProgress;
        int stageIndex = Mathf.FloorToInt(progress * damageStages.Length);
        stageIndex = Mathf.Clamp(stageIndex, 0, damageStages.Length - 1);

        for (int i = 0; i < damageStages.Length; i++)
        {
            if (damageStages[i] != null)
            {
                damageStages[i].SetActive(i == stageIndex);
            }
        }
    }

    /// <summary>
    /// Receive chop damage from an axe
    /// </summary>
    public void ReceiveChop(float amount)
    {
        if (!CanChop) return;

        currentChop += amount;
        StartOrExtendPulse();
        PlayChopSound();
        SpawnEffect(chopEffectPrefab);

        UpdateVisibleStage();

        if (currentChop >= chopRequired)
        {
            FallTree();
        }
    }

    private void StartOrExtendPulse()
    {
        pulseEndTime = Time.time + pulseDuration;

        if (pulseTween != null) return;

        // Find active stage to pulse
        GameObject activeStage = null;
        if (damageStages != null)
        {
            foreach (var stage in damageStages)
            {
                if (stage != null && stage.activeSelf)
                {
                    activeStage = stage;
                    break;
                }
            }
        }
        if (activeStage == null) return;

        Transform t = activeStage.transform;
        originalStageScale = t.localScale;
        t.localScale = originalStageScale;

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

    private void FallTree()
    {
        if (isFallen) return;

        Debug.Log($"[Tree] {name} falling! ChopHealth: {ChopHealth}");

        isFallen = true;
        StopPulse();

        PlayFallSound();
        SpawnEffect(fallEffectPrefab);

        // Find the active stage to animate falling
        GameObject activeStage = null;
        if (damageStages != null && damageStages.Length > 0)
        {
            Debug.Log($"[Tree] damageStages has {damageStages.Length} elements");
            foreach (var stage in damageStages)
            {
                if (stage != null && stage.activeSelf)
                {
                    activeStage = stage;
                    Debug.Log($"[Tree] Found active stage: {stage.name}");
                    break;
                }
            }
        }
        else
        {
            Debug.LogWarning($"[Tree] {name}: damageStages array is empty! Assign your tree model to damageStages[0] in the Inspector.");
        }

        if (activeStage != null)
        {
            Transform t = activeStage.transform;

            // Random fall direction
            Vector3 fallDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            Vector3 fallAxis = Vector3.Cross(Vector3.up, fallDirection);

            Debug.Log($"[Tree] Animating fall for {activeStage.name}");
            t.DORotate(fallAxis * fallAngle, fallDuration, RotateMode.LocalAxisAdd)
                .SetEase(Ease.InQuad)
                .OnComplete(OnTreeFallen);
        }
        else
        {
            Debug.LogWarning($"[Tree] {name}: No active stage found to animate! Skipping to OnTreeFallen.");
            OnTreeFallen();
        }
    }

    private void OnTreeFallen()
    {
        // Hide all damage stages
        if (damageStages != null)
        {
            foreach (var stage in damageStages)
            {
                if (stage != null)
                {
                    stage.SetActive(false);
                }
            }
        }

        // Show stump with scale animation
        if (stumpVisual != null)
        {
            stumpVisual.SetActive(true);
            Transform t = stumpVisual.transform;
            Vector3 originalStumpScale = t.localScale;
            t.localScale = originalStumpScale * 0.1f;
            t.DOScale(originalStumpScale, 0.3f).SetEase(Ease.OutBack);
        }

        SpawnLogs();
    }

    private void SpawnLogs()
    {
        if (logPrefab == null) return;

        Vector3 spawnPos = transform.position + logSpawnOffset;

        for (int i = 0; i < logsToSpawn; i++)
        {
            GameObject log = Instantiate(logPrefab, spawnPos, Random.rotation);

            Rigidbody rb = log.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 scatterDir = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(0.5f, 1f),
                    Random.Range(-1f, 1f)
                ).normalized;

                rb.AddForce(scatterDir * logScatterForce + Vector3.up * logScatterUpForce, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);
            }
        }
    }

    private void PlayChopSound()
    {
        if (chopSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound(chopSound, minPitch, maxPitch);
        }
    }

    private void PlayFallSound()
    {
        if (fallSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound(fallSound, minPitch, maxPitch);
        }
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
    /// Reset tree to initial state (for respawning)
    /// </summary>
    public void ResetTree()
    {
        StopPulse();
        isFallen = false;
        currentChop = 0f;

        if (stumpVisual != null)
        {
            stumpVisual.SetActive(false);
        }

        // Reset rotation of all stages
        if (damageStages != null)
        {
            foreach (var stage in damageStages)
            {
                if (stage != null)
                {
                    stage.transform.localRotation = Quaternion.identity;
                }
            }
        }

        UpdateVisibleStage();
    }
}
