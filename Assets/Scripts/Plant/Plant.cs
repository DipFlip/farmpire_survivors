using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages plant growth through watering. When enough water is received,
/// the plant evolves to the next growth stage by instantiating the next prefab.
/// </summary>
public class Plant : MonoBehaviour
{
    [Header("Growth Settings")]
    [Tooltip("Amount of water needed to advance to the next level")]
    [SerializeField] private float waterToNextLevel = 50f;

    [Tooltip("Prefab to spawn when this plant reaches full water (null if max level)")]
    [SerializeField] private GameObject nextLevelPrefab;

    [Header("Current State")]
    [SerializeField] private float currentWater = 0f;

    [Header("Events")]
    public UnityEvent<float> OnWaterReceived;
    public UnityEvent OnLevelUp;

    /// <summary>
    /// Current water amount (0 to waterToNextLevel)
    /// </summary>
    public float CurrentWater => currentWater;

    /// <summary>
    /// Water needed to reach next level
    /// </summary>
    public float WaterToNextLevel => waterToNextLevel;

    /// <summary>
    /// Progress from 0 to 1 toward next level
    /// </summary>
    public float Progress => Mathf.Clamp01(currentWater / waterToNextLevel);

    /// <summary>
    /// Whether this plant can still grow (has a next level prefab)
    /// </summary>
    public bool CanGrow => nextLevelPrefab != null;

    /// <summary>
    /// Add water to this plant. If threshold is reached, evolves to next level.
    /// </summary>
    /// <param name="amount">Amount of water to add</param>
    public void ReceiveWater(float amount)
    {
        if (!CanGrow) return;

        currentWater += amount;
        OnWaterReceived?.Invoke(currentWater);

        if (currentWater >= waterToNextLevel)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        OnLevelUp?.Invoke();

        // Spawn the next level plant at this position and rotation
        GameObject newPlant = Instantiate(nextLevelPrefab, transform.position, transform.rotation);

        // Preserve parent if any
        if (transform.parent != null)
        {
            newPlant.transform.SetParent(transform.parent);
        }

        // Destroy this plant
        Destroy(gameObject);
    }

    /// <summary>
    /// Reset water to zero (useful for testing or special mechanics)
    /// </summary>
    public void ResetWater()
    {
        currentWater = 0f;
        OnWaterReceived?.Invoke(currentWater);
    }
}
