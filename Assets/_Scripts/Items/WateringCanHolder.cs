using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach to the player to pick up and manage watering cans.
/// Handles automatic pickup, orbit distribution, and provides movement direction to cans.
/// </summary>
public class WateringCanHolder : MonoBehaviour
{
    [Header("Pickup")]
    [SerializeField] private float pickupRadius = 2f;
    [SerializeField] private LayerMask pickupLayerMask = ~0;

    [Header("Movement")]
    [Tooltip("If assigned, uses this for movement direction. Otherwise calculates from position delta.")]
    [SerializeField] private CharacterController characterController;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    private List<WateringCan> equippedCans = new List<WateringCan>();
    private Vector3 lastPosition;
    private Vector3 moveDirection;

    public IReadOnlyList<WateringCan> EquippedCans => equippedCans;
    public int CanCount => equippedCans.Count;

    private void Awake()
    {
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }
        lastPosition = transform.position;
    }

    private void Update()
    {
        UpdateMoveDirection();
        CheckForPickups();
        UpdateCans();
    }

    private void UpdateMoveDirection()
    {
        // Calculate movement direction from velocity or position delta
        if (characterController != null && characterController.velocity.sqrMagnitude > 0.01f)
        {
            Vector3 vel = characterController.velocity;
            vel.y = 0;
            if (vel.sqrMagnitude > 0.01f)
            {
                moveDirection = vel.normalized;
            }
        }
        else
        {
            Vector3 delta = transform.position - lastPosition;
            delta.y = 0;
            if (delta.sqrMagnitude > 0.0001f)
            {
                moveDirection = delta.normalized;
            }
        }

        lastPosition = transform.position;
    }

    private void CheckForPickups()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, pickupRadius, pickupLayerMask);

        foreach (var collider in colliders)
        {
            WateringCan can = collider.GetComponent<WateringCan>();
            if (can == null) can = collider.GetComponentInParent<WateringCan>();

            if (can != null && can.CurrentState == ItemState.Pickup)
            {
                PickupCan(can);
            }
        }
    }

    private void PickupCan(WateringCan can)
    {
        if (equippedCans.Contains(can)) return;

        equippedCans.Add(can);

        // Calculate orbit angle for this can
        float angle = CalculateOrbitAngle(equippedCans.Count - 1);
        can.Equip(transform, angle);

        // Redistribute all cans evenly
        RedistributeOrbitAngles();
    }

    /// <summary>
    /// Drop a specific can at a position
    /// </summary>
    public void DropCan(WateringCan can, Vector3 position)
    {
        if (!equippedCans.Contains(can)) return;

        equippedCans.Remove(can);
        can.Drop(position);

        // Redistribute remaining cans
        RedistributeOrbitAngles();
    }

    /// <summary>
    /// Drop all cans at the current position
    /// </summary>
    public void DropAllCans()
    {
        for (int i = equippedCans.Count - 1; i >= 0; i--)
        {
            WateringCan can = equippedCans[i];
            equippedCans.RemoveAt(i);
            can.Drop(transform.position + Random.insideUnitSphere * 0.5f);
        }
    }

    private void UpdateCans()
    {
        foreach (var can in equippedCans)
        {
            can.SetMoveDirection(moveDirection);
        }
    }

    private void RedistributeOrbitAngles()
    {
        for (int i = 0; i < equippedCans.Count; i++)
        {
            float angle = CalculateOrbitAngle(i);
            equippedCans[i].SetTargetOrbitAngle(angle);
        }
    }

    private float CalculateOrbitAngle(int index)
    {
        if (equippedCans.Count <= 1) return 0f;

        // Distribute evenly around the player
        return (360f / equippedCans.Count) * index;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        // Draw pickup radius
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, pickupRadius);

        // Draw movement direction
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position + Vector3.up, moveDirection * 2f);
        }
    }
}
