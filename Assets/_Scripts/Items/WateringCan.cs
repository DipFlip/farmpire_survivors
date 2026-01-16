using UnityEngine;

/// <summary>
/// A watering can that targets Plants and shoots water projectiles.
/// </summary>
public class WateringCan : HoldableItem
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
}
