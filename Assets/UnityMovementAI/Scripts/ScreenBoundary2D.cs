using UnityEngine;

namespace UnityMovementAI
{
    [RequireComponent(typeof(Collider2D))]
    public class ScreenBoundary2D : MonoBehaviour
    {
        [SerializeField]
        private Camera targetCamera;

        private Vector2 bottomLeft;
        private Vector2 topRight;
        private Vector2 widthHeight;
        private bool isInitialized;

        private void Awake()
        {
            // Ensure we have a camera reference
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null)
                {
                    Debug.LogError("No camera found for ScreenBoundary2D. Please assign a camera or ensure there is a MainCamera tagged camera in the scene.", this);
                    enabled = false;
                    return;
                }
            }

            // Ensure we have a 2D collider set as trigger
            if (!TryGetComponent<Collider2D>(out var collider) || !collider.isTrigger)
            {
                Debug.LogError("ScreenBoundary2D requires a Trigger Collider2D component.", this);
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
            float distAway = Mathf.Abs(targetCamera.transform.position.z);
            
            // Convert viewport to world coordinates
            Vector3 bottomLeftWorld = targetCamera.ViewportToWorldPoint(new Vector3(0, 0, distAway));
            Vector3 topRightWorld = targetCamera.ViewportToWorldPoint(new Vector3(1, 1, distAway));
            
            // Store as Vector2 since we're only using x/y in 2D
            bottomLeft = new Vector2(bottomLeftWorld.x, bottomLeftWorld.y);
            topRight = new Vector2(topRightWorld.x, topRightWorld.y);
            widthHeight = topRight - bottomLeft;

            // Update collider scale to match screen bounds
            transform.localScale = new Vector3(widthHeight.x, widthHeight.y, transform.localScale.z);
            
            isInitialized = true;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            KeepInBounds(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            KeepInBounds(other);
        }

        private void KeepInBounds(Collider2D other)
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

            // Check Y boundaries
            if (t.position.y < bottomLeft.y)
            {
                newPosition.y += widthHeight.y;
                positionChanged = true;
            }
            else if (t.position.y > topRight.y)
            {
                newPosition.y -= widthHeight.y;
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
            if (TryGetComponent<Collider2D>(out var collider))
            {
                collider.isTrigger = true;
            }
        }

        private void OnDrawGizmos()
        {
            if (!isInitialized) return;

            // Draw boundary visualization
            Gizmos.color = Color.cyan;
            Vector3 center = new Vector3((bottomLeft.x + topRight.x) / 2f, 
                                      (bottomLeft.y + topRight.y) / 2f, 
                                      transform.position.z);
            Vector3 size = new Vector3(widthHeight.x, widthHeight.y, 0.1f);
            Gizmos.DrawWireCube(center, size);
        }
        #endif
    }
}