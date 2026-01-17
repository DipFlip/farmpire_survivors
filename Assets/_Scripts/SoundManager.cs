using UnityEngine;

public class SoundManager : MonoBehaviour
{
  public static SoundManager Instance { get; private set; }

  [SerializeField] private AudioSource musicSource;
  [SerializeField] private AudioSource effectsSource;
  [SerializeField] private AudioClip menuMusic;
  [SerializeField] private AudioClip mainMusic;

  private bool isMusicEnabled = true;
  private bool isSoundEffectsEnabled = true;
  private float musicVolume = 1f;
  private float effectsVolume = 1f;

  private void Awake()
  {
    // Singleton pattern implementation
    if (Instance == null)
    {
      Instance = this;
      DontDestroyOnLoad(gameObject);
    }
    else
    {
      Destroy(gameObject);
    }
  }

  public void PlaySound(AudioClip clip, float minPitch = 1f, float maxPitch = 1f)
  {
    Debug.Log($"[SoundManager] PlaySound called - clip: {(clip != null ? clip.name : "NULL")}, effectsSource: {(effectsSource != null ? "OK" : "NULL")}, enabled: {isSoundEffectsEnabled}");
    if (clip != null && isSoundEffectsEnabled)
    {
      if (effectsSource == null)
      {
        Debug.LogError("[SoundManager] effectsSource is not assigned!");
        return;
      }
      float pitch = Random.Range(minPitch, maxPitch);
      Debug.Log($"[SoundManager] Playing '{clip.name}' with pitch {pitch:F2}, volume: {effectsSource.volume}, mute: {effectsSource.mute}, AudioListener exists: {FindObjectOfType<AudioListener>() != null}");
      effectsSource.pitch = pitch;
      effectsSource.PlayOneShot(clip, 1f);
    }
  }

  public void PlaySound(AudioClip[] clips, float minPitch = 1f, float maxPitch = 1f)
  {
    if (clips != null && clips.Length > 0 && isSoundEffectsEnabled)
    {
      AudioClip randomClip = clips[Random.Range(0, clips.Length)];
      if (randomClip != null)
      {
        effectsSource.pitch = Random.Range(minPitch, maxPitch);
        effectsSource.PlayOneShot(randomClip);
      }
    }
  }

  public void PlayMusic(AudioClip clip)
  {
    if (clip != null && isMusicEnabled)
    {
      musicSource.clip = clip;
      musicSource.loop = true;
      musicSource.Play();
    }
  }

  public void StopMusic()
  {
    musicSource.Stop();
  }

  public void SetMusicVolume(float volume)
  {
    musicVolume = Mathf.Clamp01(volume);
    musicSource.volume = isMusicEnabled ? musicVolume : 0f;
  }

  public void SetEffectsVolume(float volume)
  {
    effectsVolume = Mathf.Clamp01(volume);
    effectsSource.volume = isSoundEffectsEnabled ? effectsVolume : 0f;
  }

  public void PlayMenuMusic()
  {
    if (menuMusic != null)
    {
      PlayMusic(menuMusic);
    }
  }

  public void PlayMainMusic()
  {
    if (mainMusic != null)
    {
      PlayMusic(mainMusic);
    }
  }

  public void ToggleMusic()
  {
    isMusicEnabled = !isMusicEnabled;
    musicSource.volume = isMusicEnabled ? musicVolume : 0f;
  }

  public void ToggleSoundEffects()
  {
    isSoundEffectsEnabled = !isSoundEffectsEnabled;
    effectsSource.volume = isSoundEffectsEnabled ? effectsVolume : 0f;
  }

  public bool IsMusicEnabled()
  {
    return isMusicEnabled;
  }

  public bool IsSoundEffectsEnabled()
  {
    return isSoundEffectsEnabled;
  }
}