using UnityEngine;

/// <summary>
/// Projectile fired by the SeedBag to plant seeds at dug DigSites.
/// Uses object pooling - should be spawned inactive and activated via Fire().
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SeedProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float seedAmount = 3f;
    [SerializeField] private float maxLifetime = 5f;

    private Transform target;
    private Vector3 targetPosition;
    private Rigidbody rb;
    private float spawnTime;
    private bool isActive;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
    }

    /// <summary>
    /// Fire this projectile at a target
    /// </summary>
    public void Fire(Transform targetTransform, float projectileSpeed = -1f, float amount = -1f)
    {
        target = targetTransform;
        targetPosition = targetTransform.position;

        if (projectileSpeed > 0) speed = projectileSpeed;
        if (amount > 0) seedAmount = amount;

        spawnTime = Time.time;
        isActive = true;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!isActive) return;

        // Check lifetime
        if (Time.time - spawnTime > maxLifetime)
        {
            Deactivate();
            return;
        }

        // Update target position if target still exists
        if (target != null)
        {
            targetPosition = target.position;
        }

        // Move toward target
        Vector3 direction = (targetPosition - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        // Face movement direction
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        // Check if reached target
        if (Vector3.Distance(transform.position, targetPosition) < 0.5f)
        {
            HitTarget();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;

        if (other.CompareTag("DigSite"))
        {
            DigSite digSite = other.GetComponentInParent<DigSite>();
            if (digSite != null)
            {
                digSite.ReceiveSeed(seedAmount);
            }
            Deactivate();
        }
    }

    private void HitTarget()
    {
        if (target != null)
        {
            DigSite digSite = target.GetComponentInParent<DigSite>();
            if (digSite != null)
            {
                digSite.ReceiveSeed(seedAmount);
            }
        }
        Deactivate();
    }

    /// <summary>
    /// Deactivate and return to pool
    /// </summary>
    public void Deactivate()
    {
        isActive = false;
        target = null;
        gameObject.SetActive(false);
    }
}
