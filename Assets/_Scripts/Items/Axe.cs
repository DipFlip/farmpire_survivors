using UnityEngine;

/// <summary>
/// An axe that targets Trees and deals chop damage through direct collision.
/// Does NOT fire projectiles - damage is from the axe's trigger collider
/// passing through the tree's collider.
///
/// Setup:
/// - Add a Collider component with isTrigger = true (BoxCollider or CapsuleCollider)
/// - Trees need BoxCollider (not trigger) and "Tree" tag
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
}
