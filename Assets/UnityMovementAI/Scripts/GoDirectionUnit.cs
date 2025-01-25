using UnityEngine;

namespace UnityMovementAI
{
    [RequireComponent(typeof(SteeringBasics))]
    public class GoDirectionUnit : MonoBehaviour
    {
        [Tooltip("Direction the unit should move in")]
        public Vector3 direction = Vector3.forward;

        private MovementAIRigidbody rb;
        private SteeringBasics steeringBasics;

        private void Awake()
        {
            rb = GetComponent<MovementAIRigidbody>();
            steeringBasics = GetComponent<SteeringBasics>();

            if (rb == null || steeringBasics == null)
            {
                Debug.LogError($"Missing required components on {gameObject.name}", this);
                enabled = false;
                return;
            }
        }

        private void FixedUpdate()
        {
            if (direction != Vector3.zero)
            {
                // Normalize direction to ensure consistent speed
                Vector3 normalizedDirection = direction.normalized;
                
                // Apply movement
                rb.Velocity = normalizedDirection * steeringBasics.maxVelocity;

                // Update rotation
                steeringBasics.LookWhereYoureGoing();

                // Debug visualization
                #if UNITY_EDITOR
                DrawDebugLines(normalizedDirection);
                #endif
            }
            else
            {
                rb.Velocity = Vector3.zero;
            }
        }

        #if UNITY_EDITOR
        private void DrawDebugLines(Vector3 normalizedDirection)
        {
            // Draw movement direction
            Debug.DrawLine(
                rb.ColliderPosition, 
                rb.ColliderPosition + (normalizedDirection * 2f), 
                Color.cyan, 
                0f, 
                false
            );
        }

        private void OnValidate()
        {
            // Ensure direction is normalized in the inspector
            if (direction != Vector3.zero && direction.magnitude != 1f)
            {
                direction = direction.normalized;
            }
        }
        #endif
    }
}