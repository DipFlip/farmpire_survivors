using UnityEngine;

/// <summary>
/// Water projectile that moves toward a target plant and delivers water on hit.
/// Designed to be pooled by WaterShooter for efficiency.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class WaterProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float speed = 15f;
    [SerializeField] private float waterAmount = 1f;
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
    /// Initialize and fire the projectile toward a target.
    /// </summary>
    /// <param name="targetTransform">The plant to target</param>
    /// <param name="projectileSpeed">Override speed (optional)</param>
    /// <param name="water">Override water amount (optional)</param>
    public void Fire(Transform targetTransform, float projectileSpeed = -1f, float water = -1f)
    {
        target = targetTransform;
        targetPosition = targetTransform.position;

        if (projectileSpeed > 0) speed = projectileSpeed;
        if (water > 0) waterAmount = water;

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

        // Optional: rotate to face direction
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        // Check if we've reached the target (simple distance check)
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        if (distanceToTarget < 0.5f)
        {
            HitTarget();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;

        // Check if we hit a plant
        if (other.CompareTag("Plant"))
        {
            Plant plant = other.GetComponent<Plant>();
            if (plant != null)
            {
                plant.ReceiveWater(waterAmount);
            }
            Deactivate();
        }
    }

    private void HitTarget()
    {
        // Deliver water if target still exists and has Plant component
        if (target != null)
        {
            Plant plant = target.GetComponent<Plant>();
            if (plant != null)
            {
                plant.ReceiveWater(waterAmount);
            }
        }
        Deactivate();
    }

    /// <summary>
    /// Deactivate projectile and return to pool
    /// </summary>
    public void Deactivate()
    {
        isActive = false;
        target = null;
        gameObject.SetActive(false);
    }
}
