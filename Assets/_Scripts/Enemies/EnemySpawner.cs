using UnityEngine;

/// <summary>
/// Spawns enemies at randomized intervals.
/// Attach to an empty GameObject to mark the spawn point.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject enemyPrefab;

    [Tooltip("Minimum time between spawns")]
    [SerializeField] private float minSpawnTime = 5f;

    [Tooltip("Maximum time between spawns")]
    [SerializeField] private float maxSpawnTime = 15f;

    [Header("Limits")]
    [Tooltip("Maximum enemies alive from this spawner. -1 = unlimited")]
    [SerializeField] private int maxEnemies = -1;

    private float nextSpawnTime;
    private int aliveCount;

    private void Start()
    {
        ScheduleNextSpawn();
    }

    private void Update()
    {
        if (Time.time >= nextSpawnTime)
        {
            TrySpawn();
            ScheduleNextSpawn();
        }
    }

    private void ScheduleNextSpawn()
    {
        float delay = Random.Range(minSpawnTime, maxSpawnTime);
        nextSpawnTime = Time.time + delay;
    }

    private void TrySpawn()
    {
        if (enemyPrefab == null) return;

        // Check limit
        if (maxEnemies >= 0 && aliveCount >= maxEnemies) return;

        GameObject enemy = Instantiate(enemyPrefab, transform.position, transform.rotation);
        aliveCount++;

        // Track when enemy dies to decrement count
        Enemy enemyComponent = enemy.GetComponent<Enemy>();
        if (enemyComponent != null)
        {
            StartCoroutine(TrackEnemy(enemy));
        }
    }

    private System.Collections.IEnumerator TrackEnemy(GameObject enemy)
    {
        while (enemy != null)
        {
            yield return null;
        }
        aliveCount--;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward);
    }
}
