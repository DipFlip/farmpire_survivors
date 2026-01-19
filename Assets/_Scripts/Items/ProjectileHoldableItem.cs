using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Abstract base class for holdable items that fire projectiles.
/// Extends HoldableItemBase with projectile pooling and firing mechanics.
/// Used by WateringCan, Shovel, SeedBag.
/// </summary>
public abstract class ProjectileHoldableItem : HoldableItemBase
{
    [Header("Shooting Stats")]
    [SerializeField] protected float fireRate = 3f;
    [SerializeField] protected float amountPerShot = 1f;
    [SerializeField] protected float projectileSpeed = 15f;

    [Header("Projectile")]
    [SerializeField] protected GameObject projectilePrefab;
    [SerializeField] protected Transform firePoint;
    [SerializeField] protected int poolSize = 10;

    [Header("Resource")]
    [SerializeField] protected ResourceType resourceType = ResourceType.None;
    [Tooltip("Max resource capacity. -1 = unlimited")]
    [SerializeField] protected float maxResource = -1f;
    [SerializeField] protected Slider fillSlider;

    // Runtime
    protected float currentResource;
    protected float lastFireTime;
    protected List<MonoBehaviour> projectilePool;
    protected Transform poolParent;

    // Public properties
    public ResourceType ResourceType => resourceType;
    public float MaxResource => maxResource;
    public float CurrentResource => currentResource;
    public bool HasLimitedResource => maxResource >= 0f;
    public bool IsEmpty => HasLimitedResource && currentResource <= 0f;

    // Abstract - subclasses define projectile behavior
    protected abstract void FireProjectile(MonoBehaviour projectile, Transform target);
    protected abstract MonoBehaviour CreatePooledProjectile();

    protected override void Awake()
    {
        base.Awake();

        if (HasLimitedResource)
        {
            currentResource = maxResource;
        }
        UpdateFillSlider();

        if (currentState == ItemState.Equipped)
        {
            InitializePool();
        }
    }

    protected override void Update()
    {
        base.Update();

        if (currentState != ItemState.Equipped || holder == null) return;

        if (currentTarget != null && Time.time >= lastFireTime + (1f / fireRate))
        {
            Fire();
            lastFireTime = Time.time;
        }
    }

    public override void Equip(Transform newHolder, float assignedOrbitAngle)
    {
        base.Equip(newHolder, assignedOrbitAngle);

        if (projectilePool == null)
        {
            InitializePool();
        }
    }

    protected void Fire()
    {
        if (currentTarget == null) return;
        if (IsEmpty) return;

        if (!TryConsumeResource(amountPerShot)) return;

        MonoBehaviour projectile = GetProjectileFromPool();
        if (projectile == null) return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        projectile.transform.position = spawnPos;
        FireProjectile(projectile, currentTarget);

        PlayPulse();
        PlayActionSound();
    }

    protected bool TryConsumeResource(float amount)
    {
        if (!HasLimitedResource) return true;

        if (currentResource < amount) return false;

        currentResource -= amount;
        UpdateFillSlider();
        return true;
    }

    public void AddResource(float amount)
    {
        if (!HasLimitedResource) return;

        currentResource = Mathf.Min(currentResource + amount, maxResource);
        UpdateFillSlider();
    }

    protected void UpdateFillSlider()
    {
        if (fillSlider == null) return;

        if (!HasLimitedResource)
        {
            fillSlider.gameObject.SetActive(false);
            return;
        }

        fillSlider.gameObject.SetActive(true);
        fillSlider.value = currentResource / maxResource;
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
}
