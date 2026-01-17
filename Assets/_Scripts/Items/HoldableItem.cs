using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Base class for holdable items (WateringCan, Shovel, SeedBag).
/// Handles pickup, orbiting, targeting, firing, and animations.
/// Subclasses define target tag and validation logic.
/// </summary>
public abstract class HoldableItem : MonoBehaviour, IHoldableItem
{
    [Header("Shooting Stats")]
    [SerializeField] protected float fireRate = 3f;
    [SerializeField] protected float amountPerShot = 1f;
    [SerializeField] protected float projectileSpeed = 15f;
    [SerializeField] protected float detectionRange = 2f;

    [Header("Projectile")]
    [SerializeField] protected GameObject projectilePrefab;
    [SerializeField] protected Transform firePoint;
    [SerializeField] protected int poolSize = 10;

    [Header("Orbit Settings")]
    [SerializeField] protected float orbitRadius = 0.5f;
    [SerializeField] protected float orbitHeight = 0f;
    [SerializeField] protected float rotationSpeed = 10f;
    [SerializeField] protected float followSpeed = 15f;

    [Header("Fire Pulse Animation")]
    [SerializeField] protected float pulseDuration = 0.15f;
    [SerializeField] protected float pulseScale = 1.2f;

    [Header("Sound")]
    [SerializeField] protected AudioClip fireSound;
    [SerializeField] protected float minPitch = 0.9f;
    [SerializeField] protected float maxPitch = 1.1f;

    [Header("Current State")]
    [SerializeField] protected ItemState currentState = ItemState.Pickup;

    // Runtime
    protected Transform holder;
    protected float orbitAngle;
    protected float targetOrbitAngle;
    protected float lastFireTime;
    protected Transform currentTarget;
    protected Vector3 lastMoveDirection = Vector3.forward;
    protected float startingHeight;
    protected Vector3 originalScale;
    protected Tween pulseTween;

    // Pooling
    protected List<MonoBehaviour> projectilePool;
    protected Transform poolParent;

    // Abstract - subclasses define these
    protected abstract string TargetTag { get; }
    protected abstract bool IsValidTarget(GameObject targetObj);
    protected abstract void FireProjectile(MonoBehaviour projectile, Transform target);

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

        if (currentState == ItemState.Equipped)
        {
            InitializePool();
        }
    }

    protected virtual void Update()
    {
        if (currentState != ItemState.Equipped || holder == null) return;

        UpdateOrbitPosition();
        UpdateRotation();
        FindClosestTarget();

        if (currentTarget != null && Time.time >= lastFireTime + (1f / fireRate))
        {
            Fire();
            lastFireTime = Time.time;
        }
    }

    public void Equip(Transform newHolder, float assignedOrbitAngle)
    {
        holder = newHolder;
        orbitAngle = assignedOrbitAngle;
        targetOrbitAngle = assignedOrbitAngle;
        currentState = ItemState.Equipped;

        if (projectilePool == null)
        {
            InitializePool();
        }
    }

    public void Drop(Vector3 position)
    {
        holder = null;
        currentState = ItemState.Pickup;
        // Use starting height instead of dropped position's Y
        transform.position = new Vector3(position.x, startingHeight, position.z);
        currentTarget = null;

        // Kill any active pulse
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

    protected void Fire()
    {
        if (currentTarget == null) return;

        MonoBehaviour projectile = GetProjectileFromPool();
        if (projectile == null) return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        projectile.transform.position = spawnPos;
        FireProjectile(projectile, currentTarget);

        // Pulse animation
        PlayFirePulse();

        // Play sound with random pitch
        if (fireSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound(fireSound, minPitch, maxPitch);
        }
    }

    protected void PlayFirePulse()
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

    protected void InitializePool()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError($"{GetType().Name} '{name}': No projectile prefab assigned!");
            return;
        }

        GameObject poolObject = new GameObject($"{name}_ProjectilePool");
        poolParent = poolObject.transform;

        projectilePool = new List<MonoBehaviour>(poolSize);

        for (int i = 0; i < poolSize; i++)
        {
            CreatePooledProjectile();
        }
    }

    protected abstract MonoBehaviour CreatePooledProjectile();

    protected MonoBehaviour GetProjectileFromPool()
    {
        if (projectilePool == null) return null;

        foreach (var projectile in projectilePool)
        {
            if (!projectile.gameObject.activeInHierarchy)
            {
                return projectile;
            }
        }

        return CreatePooledProjectile();
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
