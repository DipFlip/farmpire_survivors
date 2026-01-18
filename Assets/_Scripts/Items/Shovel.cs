using UnityEngine;

/// <summary>
/// A shovel that targets undig DigSites and shoots dig projectiles.
/// </summary>
public class Shovel : ProjectileHoldableItem
{
    protected override string TargetTag => "DigSite";

    protected override bool IsValidTarget(GameObject targetObj)
    {
        DigSite digSite = targetObj.GetComponentInParent<DigSite>();
        return digSite != null && digSite.CanDig;
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
