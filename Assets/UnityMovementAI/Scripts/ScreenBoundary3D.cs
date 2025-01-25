using UnityEngine;

namespace UnityMovementAI
{
    [RequireComponent(typeof(Collider))]
    public class ScreenBoundary3D : MonoBehaviour
    {
        [SerializeField]
        private Camera targetCamera;

        private Vector3 bottomLeft;
        private Vector3 topRight;
        private Vector3 widthHeight;
        private bool isInitialized;

        private void Awake()
        {
            // Ensure we have a camera reference
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null)
                {
                    Debug.LogError("No camera found for ScreenBoundary3D. Please assign a camera or ensure there is a MainCamera tagged camera in the scene.", this);
                    enabled = false;
                    return;
                }
            }

            // Ensure we have a collider
            if (!TryGetComponent<Collider>(out var collider) || !collider.isTrigger)
            {
                Debug.LogError("ScreenBoundary3D requires a Trigger Collider component.", this);
                enabled = false;
                return;
            }
        }

        private void Start()
        {
            InitializeBoundary();
        }

        private void OnEnable()
        {
            if (isInitialized)
            {
                InitializeBoundary();
            }
        }

        private void InitializeBoundary()
        {
            if (targetCamera == null) return;

            // Calculate boundary based on camera position
            float distAway = Mathf.Abs(targetCamera.transform.position.y);
            
            // Convert viewport to world coordinates
            bottomLeft = targetCamera.ViewportToWorldPoint(new Vector3(0, 0, distAway));
            topRight = targetCamera.ViewportToWorldPoint(new Vector3(1, 1, distAway));
            widthHeight = topRight - bottomLeft;

            // Update collider scale to match screen bounds
            transform.localScale = new Vector3(widthHeight.x, transform.localScale.y, widthHeight.z);
            
            isInitialized = true;
        }

        private void OnTriggerStay(Collider other)
        {
            KeepInBounds(other);
        }

        private void OnTriggerExit(Collider other)
        {
            KeepInBounds(other);
        }

        private void KeepInBounds(Collider other)
        {
            if (!isInitialized) return;

            Transform t = other.transform;
            Vector3 newPosition = t.position;
            bool positionChanged = false;

            // Check X boundaries
            if (t.position.x < bottomLeft.x)
            {
                newPosition.x += widthHeight.x;
                positionChanged = true;
            }
            else if (t.position.x > topRight.x)
            {
                newPosition.x -= widthHeight.x;
                positionChanged = true;
            }

            // Check Z boundaries
            if (t.position.z < bottomLeft.z)
            {
                newPosition.z += widthHeight.z;
                positionChanged = true;
            }
            else if (t.position.z > topRight.z)
            {
                newPosition.z -= widthHeight.z;
                positionChanged = true;
            }

            // Only update position if needed
            if (positionChanged)
            {
                t.position = newPosition;
            }
        }

        #if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure collider is set to trigger in editor
            if (TryGetComponent<Collider>(out var collider))
            {
                collider.isTrigger = true;
            }
        }

        private void OnDrawGizmos()
        {
            if (!isInitialized) return;

            // Draw boundary visualization
            Gizmos.color = Color.yellow;
            Vector3 center = (bottomLeft + topRight) / 2f;
            Vector3 size = new Vector3(widthHeight.x, 1f, widthHeight.z);
            Gizmos.DrawWireCube(center, size);
        }
        #endif
    }
}