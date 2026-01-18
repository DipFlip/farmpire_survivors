using UnityEngine;
using DG.Tweening;

/// <summary>
/// Abstract base class for all holdable items.
/// Handles pickup, orbiting, targeting detection, and animations.
/// Subclasses define target tag and validation logic.
///
/// Inheritance hierarchy:
/// - ProjectileHoldableItem: Items that fire projectiles (WateringCan, Shovel, SeedBag)
/// - MeleeHoldableItem: Items that deal damage via collision (Axe)
/// - CollectorHoldableItem: Items that collect CollectableItems (Collector)
/// </summary>
public abstract class HoldableItemBase : MonoBehaviour, IHoldableItem
{
    [Header("Detection")]
    [SerializeField] protected float detectionRange = 2f;

    [Header("Orbit Settings")]
    [SerializeField] protected float orbitRadius = 0.5f;
    [SerializeField] protected float orbitHeight = 0f;
    [SerializeField] protected float rotationSpeed = 10f;
    [SerializeField] protected float followSpeed = 15f;

    [Header("Pulse Animation")]
    [SerializeField] protected float pulseDuration = 0.15f;
    [SerializeField] protected float pulseScale = 1.2f;

    [Header("Sound")]
    [SerializeField] protected AudioClip actionSound;
    [SerializeField] protected float minPitch = 0.9f;
    [SerializeField] protected float maxPitch = 1.1f;

    [Header("Current State")]
    [SerializeField] protected ItemState currentState = ItemState.Pickup;

    // Runtime
    protected Transform holder;
    protected float orbitAngle;
    protected float targetOrbitAngle;
    protected Transform currentTarget;
    protected Vector3 lastMoveDirection = Vector3.forward;
    protected float startingHeight;
    protected Vector3 originalScale;
    protected Tween pulseTween;

    // Abstract - subclasses define these
    protected abstract string TargetTag { get; }
    protected abstract bool IsValidTarget(GameObject targetObj);

    // IHoldableItem implementation
    public ItemState CurrentState => currentState;
    public Transform CurrentTarget => currentTarget;
    public bool HasTarget => currentTarget != null;
    string IHoldableItem.TargetTag => TargetTag;
    public Transform Transform => transform;

    protected virtual void Awake()
    {
        startingHeight = transform.position.y;
        originalScale = transform.localScale;
    }

    protected virtual void Update()
    {
        if (currentState != ItemState.Equipped || holder == null) return;

        UpdateOrbitPosition();
        UpdateRotation();
        FindClosestTarget();
    }

    public virtual void Equip(Transform newHolder, float assignedOrbitAngle)
    {
        holder = newHolder;
        orbitAngle = assignedOrbitAngle;
        targetOrbitAngle = assignedOrbitAngle;
        currentState = ItemState.Equipped;
    }

    public virtual void Drop(Vector3 position)
    {
        holder = null;
        currentState = ItemState.Pickup;
        transform.position = new Vector3(position.x, startingHeight, position.z);
        currentTarget = null;

        pulseTween?.Kill();
        transform.localScale = originalScale;
    }

    public void SetMoveDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude > 0.01f)
        {
            lastMoveDirection = direction.normalized;
        }
    }

    public void SetTargetOrbitAngle(float angle)
    {
        targetOrbitAngle = angle;
    }

    protected void UpdateOrbitPosition()
    {
        Vector3 faceDirection;
        if (currentTarget != null)
        {
            faceDirection = (currentTarget.position - holder.position).normalized;
        }
        else
        {
            faceDirection = lastMoveDirection;
        }

        float baseAngle = Mathf.Atan2(faceDirection.x, faceDirection.z) * Mathf.Rad2Deg;
        float desiredAngle = baseAngle + targetOrbitAngle;

        orbitAngle = Mathf.LerpAngle(orbitAngle, desiredAngle, rotationSpeed * Time.deltaTime);

        float rad = orbitAngle * Mathf.Deg2Rad;
        Vector3 orbitOffset = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * orbitRadius;
        orbitOffset.y = orbitHeight;

        Vector3 targetPos = holder.position + orbitOffset;
        transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);
    }

    protected void UpdateRotation()
    {
        Vector3 lookDirection;

        if (currentTarget != null)
        {
            lookDirection = (currentTarget.position - transform.position).normalized;
        }
        else
        {
            lookDirection = lastMoveDirection;
        }

        if (lookDirection.sqrMagnitude > 0.01f)
        {
            lookDirection.y = 0;
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    protected void FindClosestTarget()
    {
        currentTarget = null;
        float closestDistance = float.MaxValue;

        Vector3 searchOrigin = holder != null ? holder.position : transform.position;
        Collider[] colliders = Physics.OverlapSphere(searchOrigin, detectionRange);

        foreach (var collider in colliders)
        {
            if (!collider.CompareTag(TargetTag)) continue;
            if (!IsValidTarget(collider.gameObject)) continue;

            float distance = Vector3.Distance(searchOrigin, collider.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                currentTarget = collider.transform;
            }
        }
    }

    protected void PlayPulse()
    {
        pulseTween?.Kill();
        transform.localScale = originalScale;

        pulseTween = transform.DOScale(originalScale * pulseScale, pulseDuration / 2f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                pulseTween = transform.DOScale(originalScale, pulseDuration / 2f)
                    .SetEase(Ease.InQuad);
            });
    }

    protected void PlayActionSound()
    {
        if (actionSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound(actionSound, minPitch, maxPitch);
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
        Vector3 origin = holder != null ? holder.position : transform.position;
        Gizmos.DrawWireSphere(origin, detectionRange);

        if (holder != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(holder.position + Vector3.up * orbitHeight, orbitRadius);
        }

        if (currentTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
    }
}
