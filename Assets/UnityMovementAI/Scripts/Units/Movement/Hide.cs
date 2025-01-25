using UnityEngine;
using System.Collections.Generic;

namespace UnityMovementAI
{
    [RequireComponent(typeof(SteeringBasics))]
    [RequireComponent(typeof(Evade))]
    public class Hide : MonoBehaviour
    {
        [Header("Hide Settings")]
        [Tooltip("Distance to maintain from the obstacle's boundary")]
        [SerializeField] private float distanceFromBoundary = 0.6f;

        [Tooltip("Maximum radius to search for hiding spots")]
        public float findRadius = 10f;

        private SteeringBasics steeringBasics;
        private Evade evade;
        private Vector3 currentBestHidingSpot;
        private bool hasHidingSpot;

        private void Awake()
        {
            steeringBasics = GetComponent<SteeringBasics>();
            evade = GetComponent<Evade>();

            if (steeringBasics == null || evade == null)
            {
                Debug.LogError($"Missing required components on {gameObject.name}", this);
                enabled = false;
            }
        }

        /// <summary>
        /// Calculate steering force to hide from target behind obstacles
        /// </summary>
        /// <param name="target">The target to hide from</param>
        /// <param name="obstacles">Collection of possible obstacles to hide behind</param>
        /// <returns>Steering force vector</returns>
        public Vector3 GetSteering(MovementAIRigidbody target, ICollection<MovementAIRigidbody> obstacles)
        {
            Vector3 hidingSpot;
            return GetSteering(target, obstacles, out hidingSpot);
        }

        /// <summary>
        /// Calculate steering force to hide from target behind obstacles
        /// </summary>
        /// <param name="target">The target to hide from</param>
        /// <param name="obstacles">Collection of possible obstacles to hide behind</param>
        /// <param name="bestHidingSpot">Out parameter for the best hiding position found</param>
        /// <returns>Steering force vector</returns>
        public Vector3 GetSteering(MovementAIRigidbody target, ICollection<MovementAIRigidbody> obstacles, out Vector3 bestHidingSpot)
        {
            if (target == null)
            {
                bestHidingSpot = transform.position;
                return Vector3.zero;
            }

            float distToClosest = Mathf.Infinity;
            bestHidingSpot = transform.position;
            hasHidingSpot = false;

            // Find the closest valid hiding spot
            foreach (var obstacle in obstacles)
            {
                if (obstacle == null) continue;

                // Skip obstacles that are too far away
                float distToObstacle = Vector3.Distance(obstacle.Position, transform.position);
                if (distToObstacle > findRadius) continue;

                Vector3 hidingSpot = GetHidingPosition(obstacle, target);
                
                // Skip invalid hiding spots
                if (!IsValidHidingSpot(hidingSpot, target)) continue;

                float dist = Vector3.Distance(hidingSpot, transform.position);
                if (dist < distToClosest)
                {
                    distToClosest = dist;
                    bestHidingSpot = hidingSpot;
                    hasHidingSpot = true;
                }
            }

            currentBestHidingSpot = bestHidingSpot;

            // If no hiding spot is found, evade the target
            if (!hasHidingSpot)
            {
                return evade.GetSteering(target);
            }

            return steeringBasics.Arrive(bestHidingSpot);
        }

        private Vector3 GetHidingPosition(MovementAIRigidbody obstacle, MovementAIRigidbody target)
        {
            // Calculate direction from target to obstacle
            Vector3 dirToObstacle = (obstacle.Position - target.Position).normalized;
            
            // Position hiding spot behind obstacle
            float distAway = obstacle.Radius + distanceFromBoundary;
            return obstacle.Position + dirToObstacle * distAway;
        }

        private bool IsValidHidingSpot(Vector3 hidingSpot, MovementAIRigidbody target)
        {
            // Check if the hiding spot is actually behind the obstacle from target's perspective
            Vector3 dirToHiding = (hidingSpot - target.Position).normalized;
            Vector3 dirToUnit = (transform.position - target.Position).normalized;
            
            // Use dot product to determine if hiding spot is behind obstacle relative to target
            return Vector3.Dot(dirToHiding, dirToUnit) > 0;
        }

        #if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure reasonable values
            distanceFromBoundary = Mathf.Max(0.1f, distanceFromBoundary);
            findRadius = Mathf.Max(1f, findRadius);
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !hasHidingSpot) return;

            // Draw line to current hiding spot
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, currentBestHidingSpot);
            
            // Draw sphere at hiding spot
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(currentBestHidingSpot, 0.5f);
        }
        #endif
    }
}