using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// An enemy that walks toward plants and eats them.
/// First eats hanging fruits one by one, then reduces plant levels until destroyed.
/// Can be damaged by water (0.2x multiplier) and axe chops (1x multiplier).
///
/// Setup:
/// - Add "Enemy" tag
/// - Add CharacterController or NavMeshAgent for movement
/// - Add Collider for being hit by axe/projectiles
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class Enemy : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float detectionRange = 20f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float gravity = -15f;

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    [SerializeField] private Slider healthSlider;

    [Header("Attack")]
    [Tooltip("Damage dealt per attack to fruits/plant levels")]
    [SerializeField] private float attackDamage = 10f;

    [Tooltip("Time between attacks in seconds")]
    [SerializeField] private float attackCooldown = 1f;

    [Header("Target Health")]
    [Tooltip("Health of each fruit before it's eaten")]
    [SerializeField] private float fruitHealth = 20f;

    [Tooltip("Health of each plant level before it's reduced")]
    [SerializeField] private float plantLevelHealth = 50f;

    [Header("Damage Taken Multipliers")]
    [SerializeField] private float waterDamageMultiplier = 0.2f;
    [SerializeField] private float chopDamageMultiplier = 1f;

    [Header("Effects")]
    [SerializeField] private GameObject deathEffectPrefab;

    [Header("Attack Animation")]
    [SerializeField] private float attackScaleForward = 1.3f;
    [SerializeField] private float attackScaleDuration = 0.15f;

    [Header("Hit Feedback")]
    [SerializeField] private float hitFlashDuration = 0.1f;
    [SerializeField] private Color hitColor = Color.red;

    private CharacterController controller;
    private Plant targetPlant;
    private CollectableItem targetFruit;
    private float lastAttackTime;
    private Vector3 originalScale;
    private bool isDead = false;
    private float verticalVelocity;

    // Track damage dealt to current target
    private float currentFruitDamage;
    private float currentLevelDamage;

    // Renderers for hit flash
    private Renderer[] renderers;
    private Color[] originalColors;

    public float Health => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        originalScale = transform.localScale;
        currentHealth = maxHealth;

        // Cache renderers for hit flash
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material.HasProperty("_Color"))
            {
                originalColors[i] = renderers[i].material.color;
            }
        }

        UpdateHealthSlider();
    }

    private void UpdateHealthSlider()
    {
        if (healthSlider == null) return;
        healthSlider.value = currentHealth / maxHealth;
    }

    private void Update()
    {
        if (isDead) return;

        ApplyGravity();
        FindTarget();
        MoveTowardTarget();
        TryAttack();
    }

    private void ApplyGravity()
    {
        if (controller.isGrounded)
        {
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }

    private void FindTarget()
    {
        // If we have a valid target plant, keep it
        if (targetPlant != null && targetPlant.CurrentLevel > 0)
        {
            return;
        }

        // Find nearest plant
        targetPlant = null;
        targetFruit = null;
        currentFruitDamage = 0f;
        currentLevelDamage = 0f;

        Plant[] plants = FindObjectsOfType<Plant>();
        float nearestDistance = detectionRange;

        foreach (Plant plant in plants)
        {
            if (plant.CurrentLevel <= 0) continue;

            float distance = Vector3.Distance(transform.position, plant.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                targetPlant = plant;
            }
        }
    }

    private void MoveTowardTarget()
    {
        if (targetPlant == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, targetPlant.transform.position);

        // Stop when in attack range
        if (distanceToTarget <= attackRange) return;

        // Move toward target
        Vector3 direction = (targetPlant.transform.position - transform.position).normalized;
        direction.y = 0f;

        controller.Move(direction * moveSpeed * Time.deltaTime);

        // Face movement direction
        if (direction.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private void TryAttack()
    {
        if (targetPlant == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, targetPlant.transform.position);
        if (distanceToTarget > attackRange) return;

        // Check cooldown
        if (Time.time < lastAttackTime + attackCooldown) return;

        lastAttackTime = Time.time;
        PerformAttack();
    }

    private void PerformAttack()
    {
        // Play attack animation (scale forward)
        PlayAttackAnimation();

        // Priority 1: Eat fruits if plant has active harvest
        if (targetPlant.HasActiveHarvest)
        {
            AttackFruit();
            return;
        }

        // Priority 2: Reduce plant levels
        AttackPlantLevel();
    }

    private void AttackFruit()
    {
        // Find a fruit to attack
        if (targetFruit == null || targetFruit.State == CollectableItem.CollectableState.Collected)
        {
            CollectableItem[] fruits = targetPlant.GetComponentsInChildren<CollectableItem>();
            foreach (var fruit in fruits)
            {
                if (fruit.State != CollectableItem.CollectableState.Collected && fruit.gameObject.activeSelf)
                {
                    targetFruit = fruit;
                    currentFruitDamage = 0f;
                    break;
                }
            }
        }

        if (targetFruit == null) return;

        // Deal damage to fruit
        currentFruitDamage += attackDamage;

        if (currentFruitDamage >= fruitHealth)
        {
            // Destroy the fruit
            targetFruit.CollectImmediate();
            targetFruit = null;
            currentFruitDamage = 0f;
        }
    }

    private void AttackPlantLevel()
    {
        if (targetPlant.CurrentLevel <= 0) return;

        // Deal damage to current plant level
        currentLevelDamage += attackDamage;

        if (currentLevelDamage >= plantLevelHealth)
        {
            currentLevelDamage = 0f;

            if (targetPlant.CurrentLevel <= 1)
            {
                // Destroy the plant at level 1
                Destroy(targetPlant.gameObject);
                targetPlant = null;
            }
            else
            {
                // Reduce plant level
                targetPlant.SetLevel(targetPlant.CurrentLevel - 1);
            }
        }
    }

    private void PlayAttackAnimation()
    {
        // Scale forward (Z axis) from the back pivot by offsetting position
        transform.DOKill();

        Vector3 attackScale = originalScale;
        attackScale.z *= attackScaleForward;

        // Calculate forward offset to make it appear to scale from back
        // When scaling from center, front moves forward by half the scale increase
        float scaleIncrease = originalScale.z * (attackScaleForward - 1f);
        float forwardOffset = scaleIncrease / 2f;

        Vector3 startPos = transform.position;
        Vector3 attackPos = startPos + transform.forward * forwardOffset;

        // Scale and move forward together
        transform.DOScale(attackScale, attackScaleDuration).SetEase(Ease.OutQuad);
        transform.DOMove(attackPos, attackScaleDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                // Return to original
                transform.DOScale(originalScale, attackScaleDuration).SetEase(Ease.InQuad);
                transform.DOMove(startPos, attackScaleDuration).SetEase(Ease.InQuad);
            });
    }

    /// <summary>
    /// Receive damage from water (multiplied by waterDamageMultiplier)
    /// </summary>
    public void ReceiveWaterDamage(float waterAmount)
    {
        if (isDead) return;

        float damage = waterAmount * waterDamageMultiplier;
        TakeDamage(damage);
    }

    /// <summary>
    /// Receive damage from chop/axe (multiplied by chopDamageMultiplier)
    /// </summary>
    public void ReceiveChopDamage(float chopAmount)
    {
        if (isDead) return;

        float damage = chopAmount * chopDamageMultiplier;
        TakeDamage(damage);
    }

    private void TakeDamage(float damage)
    {
        currentHealth -= damage;
        UpdateHealthSlider();
        PlayHitFlash();

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void PlayHitFlash()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material.HasProperty("_Color"))
            {
                renderers[i].material.color = hitColor;

                int index = i;
                DOVirtual.DelayedCall(hitFlashDuration, () =>
                {
                    if (renderers[index] != null)
                    {
                        renderers[index].material.color = originalColors[index];
                    }
                });
            }
        }
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;

        // Spawn death effect
        if (deathEffectPrefab != null)
        {
            GameObject effect = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

            if (effect.TryGetComponent<ParticleSystem>(out var ps))
            {
                Destroy(effect, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(effect, 3f);
            }
        }

        // Death animation then destroy
        transform.DOScale(Vector3.zero, 0.3f)
            .SetEase(Ease.InBack)
            .OnComplete(() => Destroy(gameObject));
    }

    private void OnDrawGizmosSelected()
    {
        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
