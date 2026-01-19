using UnityEngine;

/// <summary>
/// A collector tool that pulls CollectableItems (like logs) toward itself.
/// Does NOT fire projectiles. Items fly toward it and are collected.
/// Has a capacity limit of 6 items by default.
/// Automatically deposits items to DepositStations when in range.
///
/// Setup:
/// - Add to a 3D model (bag, magnet, etc.)
/// - Configure collection range and capacity
/// - Items need "Collectable" tag
/// - Add child with trigger collider for deposit detection
/// </summary>
public class Collector : CollectorHoldableItem
{
    [Header("Collector Sounds")]
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private AudioClip depositSound;
    [SerializeField] private float collectMinPitch = 0.9f;
    [SerializeField] private float collectMaxPitch = 1.1f;

    protected override void OnItemCollected(string itemType)
    {
        base.OnItemCollected(itemType);

        if (collectSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound(collectSound, collectMinPitch, collectMaxPitch);
        }
    }

    protected override void OnItemDeposited(string itemType)
    {
        base.OnItemDeposited(itemType);

        if (depositSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound(depositSound, collectMinPitch, collectMaxPitch);
        }
    }
}
