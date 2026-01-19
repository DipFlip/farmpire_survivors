using UnityEngine;

/// <summary>
/// Helper script for Collector's deposit detection collider.
/// Place on a child GameObject with a trigger collider.
/// Forwards trigger events to the parent CollectorHoldableItem.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DepositDetector : MonoBehaviour
{
    private CollectorHoldableItem collector;

    private void Awake()
    {
        collector = GetComponentInParent<CollectorHoldableItem>();

        if (collector == null)
        {
            Debug.LogError($"[DepositDetector] No CollectorHoldableItem found on parent of {name}!");
        }

        // Ensure collider is trigger
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collector == null) return;

        DepositStation station = other.GetComponent<DepositStation>();
        if (station != null)
        {
            collector.OnDepositStationEnter(station);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (collector == null) return;

        DepositStation station = other.GetComponent<DepositStation>();
        if (station != null)
        {
            collector.OnDepositStationExit(station);
        }
    }
}
