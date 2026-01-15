using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Manages plant growth through watering. Growth stages are child GameObjects
/// that get enabled/disabled as the plant levels up.
///
/// Setup:
/// - Parent: This script
/// - Children: Each growth level prefab with mesh + BoxCollider + "Plant" tag
/// - Assign children to growthStages array in order
/// </summary>
public class Plant : MonoBehaviour
{
    [Header("Growth Stages")]
    [Tooltip("Child GameObjects for each growth level (assign in order: Lv1, Lv2, Lv3, etc.)")]
    [SerializeField] private GameObject[] growthStages;

    [Header("Growth Settings")]
    [Tooltip("Amount of water needed to advance to the next level")]
    [SerializeField] private float waterPerLevel = 50f;

    [Header("Current State")]
    [Tooltip("Current growth level (1 = first stage)")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private float currentWater = 0f;

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
    public bool CanGrow => currentLevel < MaxLevel;

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

    /// <summary>
    /// Add water to this plant. If threshold is reached, evolves to next level.
    /// </summary>
    public void ReceiveWater(float amount)
    {
        if (!CanGrow) return;

        currentWater += amount;

        if (currentWater >= waterPerLevel)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        if (!CanGrow) return;

        // Disable current stage (convert 1-indexed to array index)
        int currentIndex = currentLevel - 1;
        if (growthStages[currentIndex] != null)
        {
            growthStages[currentIndex].SetActive(false);
        }

        // Advance level
        currentLevel++;
        currentWater = 0f;

        // Enable new stage
        int newIndex = currentLevel - 1;
        if (growthStages[newIndex] != null)
        {
            growthStages[newIndex].SetActive(true);
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
