using UnityEngine;
using UnityEngine.InputSystem;

namespace UnityMovementAI
{
    [RequireComponent(typeof(MovementAIRigidbody))]
    [RequireComponent(typeof(Camera))]
    [RequireComponent(typeof(PlayerInput))]
    public class ThirdPersonUnit : MonoBehaviour
    {
        public float speed = 5f;
        public float facingSpeed = 720f;
        public float jumpSpeed = 7f;
        public bool autoAttachToCamera = true;

        private MovementAIRigidbody rb;
        private Transform cam;
        private Vector2 moveInput;
        private bool jumpRequested;

        private void Start()
        {
            rb = GetComponent<MovementAIRigidbody>();
            // Using GetComponent to handle multiple cameras better
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cam = mainCamera.transform;
                if (autoAttachToCamera)
                {
                    var thirdPersonCam = cam.GetComponent<ThirdPersonCamera>();
                    if (thirdPersonCam != null)
                    {
                        thirdPersonCam.target = transform;
                    }
                }
            }
            else
            {
                Debug.LogError("No main camera found in the scene!");
            }
        }

        // New Input System callbacks
        public void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
        }

        public void OnJump(InputValue value)
        {
            if (value.isPressed)
            {
                jumpRequested = true;
            }
        }

        private void Update()
        {
            if (jumpRequested)
            {
                rb.Jump(jumpSpeed);
                jumpRequested = false;
            }
        }

        private void FixedUpdate()
        {
            if (Cursor.lockState == CursorLockMode.Locked && cam != null)
            {
                rb.Velocity = GetMovementDir() * speed;
            }
            else
            {
                rb.Velocity = Vector3.zero;
            }
        }

        private void LateUpdate()
        {
            if (Cursor.lockState == CursorLockMode.Locked && cam != null)
            {
                Vector3 dir = GetMovementDir();

                if (dir.magnitude > 0)
                {
                    float curFacing = transform.eulerAngles.y;
                    float facing = Mathf.Atan2(-dir.z, dir.x) * Mathf.Rad2Deg;
                    rb.Rotation = Quaternion.Euler(0, Mathf.MoveTowardsAngle(curFacing, facing, facingSpeed * Time.deltaTime), 0);
                }
            }
        }

        private Vector3 GetMovementDir()
        {
            // Convert input to camera-relative movement
            if (cam != null)
            {
                Vector3 forward = cam.forward;
                Vector3 right = cam.right;

                // Project vectors onto the horizontal plane
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();

                return (forward * moveInput.y + right * moveInput.x).normalized;
            }
            return Vector3.zero;
        }
    }
}