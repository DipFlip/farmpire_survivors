using UnityEngine;

/// <summary>
/// An axe that targets Trees and Enemies, dealing chop damage through direct collision.
/// Does NOT fire projectiles - damage is from the axe's trigger collider
/// passing through the target's collider.
///
/// Setup:
/// - Add a Collider component with isTrigger = true (BoxCollider or CapsuleCollider)
/// - Trees need BoxCollider (not trigger) and "Tree" tag
/// - Enemies need Collider and "Enemy" tag
/// </summary>
public class Axe : MeleeHoldableItem
{
    protected override string TargetTag => "Tree";

    protected override bool IsValidTarget(GameObject targetObj)
    {
        Tree tree = targetObj.GetComponentInParent<Tree>();
        return tree != null && tree.CanChop;
    }

    protected override void DealDamage(GameObject targetObj, float amount)
    {
        Tree tree = targetObj.GetComponentInParent<Tree>();
        if (tree != null)
        {
            tree.ReceiveChop(amount);
        }
    }

    protected override void OnTriggerEnter(Collider other)
    {
        // First try normal Tree targeting
        base.OnTriggerEnter(other);

        // Also check for Enemy
        if (currentState != ItemState.Equipped) return;

        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponentInParent<Enemy>();
            if (enemy != null && !enemy.IsDead)
            {
                // Check cooldown for this specific target
                if (hitCooldowns.TryGetValue(other, out float lastHitTime))
                {
                    if (Time.time < lastHitTime + hitCooldown)
                    {
                        return;
                    }
                }

                enemy.ReceiveChopDamage(damageAmount);
                hitCooldowns[other] = Time.time;

                PlayPulse();
                PlayActionSound();
            }
        }
    }
}
