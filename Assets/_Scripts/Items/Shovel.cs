using UnityEngine;

/// <summary>
/// A shovel that targets undig DigSites and shoots dig projectiles.
/// Hovers over the target DigSite while actively digging.
/// </summary>
public class Shovel : ProjectileHoldableItem
{
    [Header("Dig Hover")]
    [SerializeField] private float hoverHeight = 1f;
    [SerializeField] private float hoverSpeed = 8f;

    protected override string TargetTag => "DigSite";

    protected override bool IsValidTarget(GameObject targetObj)
    {
        DigSite digSite = targetObj.GetComponentInParent<DigSite>();
        return digSite != null && digSite.CanDig;
    }

    protected override void UpdateOrbitPosition()
    {
        if (IsRefilling)
        {
            base.UpdateOrbitPosition();
            return;
        }

        if (currentTarget != null)
        {
            Vector3 hoverPos = currentTarget.position + Vector3.up * hoverHeight;
            transform.position = Vector3.Lerp(transform.position, hoverPos, hoverSpeed * Time.deltaTime);
            return;
        }

        base.UpdateOrbitPosition();
    }

    /// <summary>
    /// Override to also find Enemy targets in addition to DigSites.
    /// Prioritizes enemies over dig sites when both are in range.
    /// </summary>
    protected new void FindClosestTarget()
    {
        currentTarget = null;
        float closestDistance = float.MaxValue;

        Vector3 searchOrigin = holder != null ? holder.position : transform.position;
        Collider[] colliders = Physics.OverlapSphere(searchOrigin, detectionRange);

        // First priority: find enemies
        foreach (var collider in colliders)
        {
            if (!collider.CompareTag("Enemy")) continue;

            Enemy enemy = collider.GetComponentInParent<Enemy>();
            if (enemy == null || enemy.IsDead) continue;

            float distance = Vector3.Distance(searchOrigin, collider.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                currentTarget = collider.transform;
            }
        }

        // If no enemy found, check for dig sites
        if (currentTarget == null)
        {
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
    }

    protected override void Update()
    {
        if (currentState != ItemState.Equipped || holder == null) return;

        UpdateOrbitPosition();
        UpdateRotation();
        if (!IsRefilling) FindClosestTarget();

        if (currentTarget != null && Time.time >= lastFireTime + (1f / fireRate))
        {
            Fire();
            lastFireTime = Time.time;
        }
    }

    protected override void FireProjectile(MonoBehaviour projectile, Transform target)
    {
        DigProjectile digProjectile = projectile as DigProjectile;
        if (digProjectile != null)
        {
            digProjectile.Fire(target, projectileSpeed, amountPerShot);
        }
    }

    protected override MonoBehaviour CreatePooledProjectile()
    {
        GameObject obj = Instantiate(projectilePrefab, poolParent);
        obj.SetActive(false);

        DigProjectile projectile = obj.GetComponent<DigProjectile>();
        if (projectile == null)
        {
            projectile = obj.AddComponent<DigProjectile>();
        }

        projectilePool.Add(projectile);
        return projectile;
    }
}
