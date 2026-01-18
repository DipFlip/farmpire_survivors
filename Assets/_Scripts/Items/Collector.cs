using UnityEngine;

/// <summary>
/// A collector tool that pulls CollectableItems (like logs) toward itself.
/// Does NOT fire projectiles. Items fly toward it and are collected.
/// Has a capacity limit of 6 items by default.
///
/// Setup:
/// - Add to a 3D model (bag, magnet, etc.)
/// - Configure collection range and capacity
/// - Items need "Collectable" tag
/// </summary>
public class Collector : CollectorHoldableItem
{
    [Header("Collector Sound")]
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private float collectMinPitch = 0.9f;
    [SerializeField] private float collectMaxPitch = 1.1f;

    protected override void OnItemCollected()
    {
        base.OnItemCollected();

        // Play collection-specific sound
        if (collectSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound(collectSound, collectMinPitch, collectMaxPitch);
        }
    }
}
