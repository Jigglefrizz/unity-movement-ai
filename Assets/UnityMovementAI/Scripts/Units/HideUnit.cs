using UnityEngine;

namespace UnityMovementAI
{
    [RequireComponent(typeof(SteeringBasics))]
    [RequireComponent(typeof(Hide))]
    [RequireComponent(typeof(WallAvoidance))]
    public class HideUnit : MonoBehaviour
    {
        [SerializeField]
        private MovementAIRigidbody target;

        [SerializeField]
        private string obstacleSpawnerName = "ObstacleSpawner";

        private SteeringBasics steeringBasics;
        private Hide hide;
        private Spawner obstacleSpawner;
        private WallAvoidance wallAvoid;
        private bool isInitialized;

        private void Awake()
        {
            // Get required components
            steeringBasics = GetComponent<SteeringBasics>();
            hide = GetComponent<Hide>();
            wallAvoid = GetComponent<WallAvoidance>();

            if (target == null)
            {
                Debug.LogWarning($"No target assigned to {nameof(HideUnit)} on {gameObject.name}", this);
            }
        }

        private void Start()
        {
            // Find the obstacle spawner
            var spawnerObj = GameObject.Find(obstacleSpawnerName);
            if (spawnerObj != null)
            {
                obstacleSpawner = spawnerObj.GetComponent<Spawner>();
                if (obstacleSpawner == null)
                {
                    Debug.LogError($"Object '{obstacleSpawnerName}' found but does not have a Spawner component!", this);
                    enabled = false;
                    return;
                }
            }
            else
            {
                Debug.LogError($"Could not find obstacle spawner object '{obstacleSpawnerName}'!", this);
                enabled = false;
                return;
            }

            isInitialized = true;
        }

        private void FixedUpdate()
        {
            if (!isInitialized || target == null || obstacleSpawner == null) return;

            // Calculate hide position and acceleration
            Vector3 hidePosition;
            Vector3 hideAccel = hide.GetSteering(target, obstacleSpawner.activeObjects, out hidePosition);

            // Calculate wall avoidance
            Vector3 accel = wallAvoid.GetSteering(hidePosition - transform.position);

            // If wall avoidance is not significant, use hide acceleration
            if (accel.magnitude < 0.005f)
            {
                accel = hideAccel;
            }

            // Apply steering and rotation
            steeringBasics.Steer(accel);
            steeringBasics.LookWhereYoureGoing();
        }

        #if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(obstacleSpawnerName))
            {
                obstacleSpawnerName = "ObstacleSpawner";
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!isInitialized || target == null || obstacleSpawner == null) return;

            // Draw line to target
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, target.transform.position);

            // Draw detection sphere
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, hide.findRadius);
        }
        #endif
    }
}