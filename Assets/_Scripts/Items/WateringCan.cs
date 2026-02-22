using UnityEngine;

/// <summary>
/// A watering can that targets Plants and Enemies, shooting water projectiles.
/// Plants receive water to grow, Enemies receive water damage.
/// </summary>
public class WateringCan : ProjectileHoldableItem
{
    protected override string TargetTag => "Plant";

    protected override bool IsValidTarget(GameObject targetObj)
    {
        Plant plant = targetObj.GetComponentInParent<Plant>();
        return plant != null && plant.CanGrow;
    }

    protected override void FireProjectile(MonoBehaviour projectile, Transform target)
    {
        WaterProjectile waterProjectile = projectile as WaterProjectile;
        if (waterProjectile != null)
        {
            waterProjectile.Fire(target, projectileSpeed, amountPerShot);
        }
    }

    protected override MonoBehaviour CreatePooledProjectile()
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

    /// <summary>
    /// Override to also find Enemy targets in addition to Plants.
    /// Prioritizes enemies over plants when both are in range.
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

        // If no enemy found, check for plants
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
}
