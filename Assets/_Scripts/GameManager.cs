using UnityEngine;

/// <summary>
/// Main game manager - handles game startup and global game state.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Play the main game music
        Debug.Log($"[GameManager] SoundManager.Instance exists: {SoundManager.Instance != null}");
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayMainMusic();
        }
    }
}
