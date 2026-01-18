using UnityEngine;

/// <summary>
/// A seed bag that targets dug DigSites and shoots seed projectiles.
/// </summary>
public class SeedBag : ProjectileHoldableItem
{
    protected override string TargetTag => "DigSite";

    protected override bool IsValidTarget(GameObject targetObj)
    {
        DigSite digSite = targetObj.GetComponentInParent<DigSite>();
        return digSite != null && digSite.CanPlant;
    }

    protected override void FireProjectile(MonoBehaviour projectile, Transform target)
    {
        SeedProjectile seedProjectile = projectile as SeedProjectile;
        if (seedProjectile != null)
        {
            seedProjectile.Fire(target, projectileSpeed, amountPerShot);
        }
    }

    protected override MonoBehaviour CreatePooledProjectile()
    {
        GameObject obj = Instantiate(projectilePrefab, poolParent);
        obj.SetActive(false);

        SeedProjectile projectile = obj.GetComponent<SeedProjectile>();
        if (projectile == null)
        {
            projectile = obj.AddComponent<SeedProjectile>();
        }

        projectilePool.Add(projectile);
        return projectile;
    }
}
