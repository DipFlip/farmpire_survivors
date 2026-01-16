using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A watering can that can be picked up and orbits the player while shooting at plants.
/// Each can has its own stats and shooting behavior.
/// </summary>
public class WateringCan : MonoBehaviour
{
    public enum State { Pickup, Equipped }

    [Header("Shooting Stats")]
    [SerializeField] private float fireRate = 3f;
    [SerializeField] private float waterPerShot = 1f;
    [SerializeField] private float projectileSpeed = 15f;
    [SerializeField] private float detectionRange = 20f;

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private int poolSize = 10;

    [Header("Orbit Settings")]
    [SerializeField] private float orbitRadius = 1.5f;
    [SerializeField] private float orbitHeight = 1f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float followSpeed = 8f;

    [Header("Current State")]
    [SerializeField] private State currentState = State.Pickup;

    // Runtime
    private Transform holder;
    private float orbitAngle;
    private float targetOrbitAngle;
    private float lastFireTime;
    private Transform currentTarget;
    private Vector3 lastMoveDirection = Vector3.forward;

    // Pooling
    private List<WaterProjectile> projectilePool;
    private Transform poolParent;

    // Public accessors
    public State CurrentState => currentState;
    public Transform CurrentTarget => currentTarget;
    public bool HasTarget => currentTarget != null;

    private void Awake()
    {
        if (currentState == State.Equipped)
        {
            InitializePool();
        }
    }

    private void Update()
    {
        if (currentState != State.Equipped || holder == null) return;

        UpdateOrbitPosition();
        UpdateRotation();
        FindClosestPlant();

        if (currentTarget != null && Time.time >= lastFireTime + (1f / fireRate))
        {
            Fire();
            lastFireTime = Time.time;
        }
    }

    /// <summary>
    /// Pick up this can and attach it to a holder
    /// </summary>
    public void Equip(Transform newHolder, float assignedOrbitAngle)
    {
        holder = newHolder;
        orbitAngle = assignedOrbitAngle;
        targetOrbitAngle = assignedOrbitAngle;
        currentState = State.Equipped;

        // Initialize pool when first equipped
        if (projectilePool == null)
        {
            InitializePool();
        }
    }

    /// <summary>
    /// Drop this can at a position
    /// </summary>
    public void Drop(Vector3 position)
    {
        holder = null;
        currentState = State.Pickup;
        transform.position = position;
        currentTarget = null;
    }

    /// <summary>
    /// Set the movement direction (for facing when not shooting)
    /// </summary>
    public void SetMoveDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude > 0.01f)
        {
            lastMoveDirection = direction.normalized;
        }
    }

    /// <summary>
    /// Update the target orbit angle (for multi-can distribution)
    /// </summary>
    public void SetTargetOrbitAngle(float angle)
    {
        targetOrbitAngle = angle;
    }

    private void UpdateOrbitPosition()
    {
        // Determine target angle based on where we need to face
        Vector3 faceDirection;
        if (currentTarget != null)
        {
            // Face toward target
            faceDirection = (currentTarget.position - holder.position).normalized;
        }
        else
        {
            // Face movement direction
            faceDirection = lastMoveDirection;
        }

        // Convert direction to angle and add offset for multiple cans
        float baseAngle = Mathf.Atan2(faceDirection.x, faceDirection.z) * Mathf.Rad2Deg;
        float desiredAngle = baseAngle + targetOrbitAngle; // targetOrbitAngle is offset for multi-can distribution

        // Smoothly interpolate to desired angle
        orbitAngle = Mathf.LerpAngle(orbitAngle, desiredAngle, rotationSpeed * Time.deltaTime);

        // Calculate orbit position
        float rad = orbitAngle * Mathf.Deg2Rad;
        Vector3 orbitOffset = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * orbitRadius;
        orbitOffset.y = orbitHeight;

        Vector3 targetPos = holder.position + orbitOffset;

        // Smoothly move to position
        transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);
    }

    private void UpdateRotation()
    {
        Vector3 lookDirection;

        if (currentTarget != null)
        {
            // Face the target
            lookDirection = (currentTarget.position - transform.position).normalized;
        }
        else
        {
            // Face movement direction
            lookDirection = lastMoveDirection;
        }

        if (lookDirection.sqrMagnitude > 0.01f)
        {
            lookDirection.y = 0; // Keep upright
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void FindClosestPlant()
    {
        currentTarget = null;
        float closestDistance = float.MaxValue;

        Vector3 searchOrigin = holder != null ? holder.position : transform.position;
        Collider[] colliders = Physics.OverlapSphere(searchOrigin, detectionRange);

        foreach (var collider in colliders)
        {
            if (!collider.CompareTag("Plant")) continue;

            Plant plant = collider.GetComponentInParent<Plant>();
            if (plant == null || !plant.CanGrow) continue;

            float distance = Vector3.Distance(searchOrigin, plant.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                currentTarget = plant.transform;
            }
        }
    }

    private void Fire()
    {
        if (currentTarget == null) return;

        WaterProjectile projectile = GetProjectileFromPool();
        if (projectile == null) return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        projectile.transform.position = spawnPos;
        projectile.Fire(currentTarget, projectileSpeed, waterPerShot);
    }

    private void InitializePool()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError($"WateringCan '{name}': No projectile prefab assigned!");
            return;
        }

        GameObject poolObject = new GameObject($"{name}_ProjectilePool");
        poolParent = poolObject.transform;

        projectilePool = new List<WaterProjectile>(poolSize);

        for (int i = 0; i < poolSize; i++)
        {
            CreatePooledProjectile();
        }
    }

    private WaterProjectile CreatePooledProjectile()
    {
        GameObject obj = Instantiate(projectilePrefab, poolParent);
        obj.SetActive(false);

        WaterProjectile projectile = obj.GetComponent<WaterProjectile>();
        if (projectile == null)
        {
            projectile = obj.AddComponent<WaterProjectile>();
        }

        projectilePool.Add(projectile);
        return projectile;
    }

    private WaterProjectile GetProjectileFromPool()
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

    private void OnDrawGizmosSelected()
    {
        // Draw detection range
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
        Vector3 origin = holder != null ? holder.position : transform.position;
        Gizmos.DrawWireSphere(origin, detectionRange);

        // Draw orbit radius
        if (holder != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(holder.position + Vector3.up * orbitHeight, orbitRadius);
        }

        // Draw line to target
        if (currentTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
    }
}
