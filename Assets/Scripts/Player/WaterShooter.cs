using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Vampire Survivors-style auto-shooter that targets the closest plant
/// and fires water projectiles automatically.
/// </summary>
public class WaterShooter : MonoBehaviour
{
    [Header("Targeting")]
    [Tooltip("Maximum distance to detect plants")]
    [SerializeField] private float detectionRange = 20f;

    [Tooltip("Tag used to identify plants")]
    [SerializeField] private string plantTag = "Plant";

    [Tooltip("Layer mask for plant detection (optional optimization)")]
    [SerializeField] private LayerMask plantLayerMask = ~0;

    [Header("Firing")]
    [Tooltip("Shots per second")]
    [SerializeField] private float fireRate = 3f;

    [Tooltip("Offset from this transform where projectiles spawn")]
    [SerializeField] private Vector3 fireOffset = new Vector3(0f, 1f, 0.5f);

    [Header("Projectile Settings")]
    [Tooltip("Projectile prefab (must have WaterProjectile component)")]
    [SerializeField] private GameObject projectilePrefab;

    [Tooltip("Speed of water projectiles")]
    [SerializeField] private float projectileSpeed = 15f;

    [Tooltip("Water delivered per projectile")]
    [SerializeField] private float waterPerShot = 1f;

    [Header("Object Pool")]
    [Tooltip("Initial pool size")]
    [SerializeField] private int poolSize = 20;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    private List<WaterProjectile> projectilePool;
    private Transform poolParent;
    private float lastFireTime;
    private Transform currentTarget;

    private void Awake()
    {
        InitializePool();
    }

    private void Update()
    {
        FindClosestPlant();

        if (currentTarget != null && Time.time >= lastFireTime + (1f / fireRate))
        {
            FireAtTarget();
            lastFireTime = Time.time;
        }
    }

    private void InitializePool()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("WaterShooter: No projectile prefab assigned!");
            return;
        }

        // Create pool parent for organization
        GameObject poolObject = new GameObject("WaterProjectilePool");
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
        // Find inactive projectile
        foreach (var projectile in projectilePool)
        {
            if (!projectile.gameObject.activeInHierarchy)
            {
                return projectile;
            }
        }

        // Pool exhausted, create new one
        return CreatePooledProjectile();
    }

    private void FindClosestPlant()
    {
        currentTarget = null;
        float closestDistance = float.MaxValue;

        // Find all colliders in range
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRange, plantLayerMask);

        foreach (var collider in colliders)
        {
            if (!collider.CompareTag(plantTag)) continue;

            // Only target plants that can still grow
            Plant plant = collider.GetComponent<Plant>();
            if (plant != null && !plant.CanGrow) continue;

            float distance = Vector3.Distance(transform.position, collider.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                currentTarget = collider.transform;
            }
        }
    }

    private void FireAtTarget()
    {
        if (currentTarget == null) return;

        WaterProjectile projectile = GetProjectileFromPool();

        // Calculate spawn position
        Vector3 spawnPos = transform.position + transform.TransformDirection(fireOffset);
        projectile.transform.position = spawnPos;

        // Fire at target
        projectile.Fire(currentTarget, projectileSpeed, waterPerShot);
    }

    /// <summary>
    /// Manually fire at a specific target (for special abilities, etc.)
    /// </summary>
    public void FireAt(Transform target)
    {
        if (target == null) return;

        WaterProjectile projectile = GetProjectileFromPool();
        Vector3 spawnPos = transform.position + transform.TransformDirection(fireOffset);
        projectile.transform.position = spawnPos;
        projectile.Fire(target, projectileSpeed, waterPerShot);
    }

    /// <summary>
    /// Get the current target (for UI, etc.)
    /// </summary>
    public Transform CurrentTarget => currentTarget;

    /// <summary>
    /// Check if there's a valid target in range
    /// </summary>
    public bool HasTarget => currentTarget != null;

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        // Draw detection range
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw fire offset
        Gizmos.color = Color.cyan;
        Vector3 firePos = transform.position + transform.TransformDirection(fireOffset);
        Gizmos.DrawWireSphere(firePos, 0.1f);

        // Draw line to current target
        if (currentTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(firePos, currentTarget.position);
        }
    }
}
