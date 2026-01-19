using UnityEngine;

namespace ithappy.Animals_FREE
{
    [RequireComponent(typeof(CreatureMover))]
    public class CreatureWander : MonoBehaviour
    {
        [Header("Wander Settings")]
        [SerializeField] private float wanderRadius = 10f;
        [SerializeField] private float minWanderTime = 2f;
        [SerializeField] private float maxWanderTime = 5f;
        [SerializeField] private float minIdleTime = 1f;
        [SerializeField] private float maxIdleTime = 3f;
        [SerializeField] private bool canRun = false;
        [SerializeField, Range(0f, 1f)] private float runChance = 0.2f;

        private CreatureMover mover;
        private Vector3 startPosition;
        private float timer;
        private bool isWandering;
        private Vector3 targetPosition;
        private bool isRunning;

        private void Awake()
        {
            mover = GetComponent<CreatureMover>();
        }

        private void Start()
        {
            startPosition = transform.position;
            PickNewState();
        }

        private void Update()
        {
            timer -= Time.deltaTime;

            if (timer <= 0f)
            {
                PickNewState();
            }

            if (isWandering)
            {
                // Walk forward toward the target position
                mover.SetInput(Vector2.up, targetPosition, isRunning, false);
            }
            else
            {
                // Idle - no movement, look ahead
                mover.SetInput(Vector2.zero, transform.position + transform.forward * 5f, false, false);
            }
        }

        private void PickNewState()
        {
            isWandering = !isWandering;

            if (isWandering)
            {
                // Pick a random point within wander radius of start position
                Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
                targetPosition = startPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);
                targetPosition.y = transform.position.y;

                isRunning = canRun && Random.value < runChance;
                timer = Random.Range(minWanderTime, maxWanderTime);
            }
            else
            {
                isRunning = false;
                timer = Random.Range(minIdleTime, maxIdleTime);
            }
        }
    }
}
