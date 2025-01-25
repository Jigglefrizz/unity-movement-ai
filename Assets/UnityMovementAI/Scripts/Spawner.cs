using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace UnityMovementAI
{
    public class Spawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private Transform objPrefab;
        [SerializeField] private Vector2 objectSizeRange = new Vector2(1, 2);
        [SerializeField] private int numberOfObjects = 10;
        [SerializeField] private bool randomizeOrientation = false;

        [Header("Placement Settings")]
        [SerializeField] private float boundaryPadding = 1f;
        [SerializeField] private float spaceBetweenObjects = 1f;
        [SerializeField] private MovementAIRigidbody[] thingsToAvoid;
        [SerializeField] private Camera targetCamera;

        [Header("Object Pooling")]
        [SerializeField] private int defaultPoolSize = 10;
        [SerializeField] private int maxPoolSize = 20;

        private Vector3 bottomLeft;
        private Vector3 widthHeight;
        private bool isObj3D;
        private ObjectPool<MovementAIRigidbody> objectPool;

        [System.NonSerialized]
        public readonly List<MovementAIRigidbody> activeObjects = new();

        private void Awake()
        {
            if (objPrefab == null)
            {
                Debug.LogError("No prefab assigned to spawn!", this);
                enabled = false;
                return;
            }

            // Setup camera reference
            targetCamera = targetCamera != null ? targetCamera : Camera.main;
            if (targetCamera == null)
            {
                Debug.LogError("No camera found for spawning! Assign a camera or tag one as MainCamera.", this);
                enabled = false;
                return;
            }

            // Initialize object pool
            objectPool = new ObjectPool<MovementAIRigidbody>(
                createFunc: CreatePooledObject,
                actionOnGet: OnTakeFromPool,
                actionOnRelease: OnReturnToPool,
                actionOnDestroy: OnDestroyPoolObject,
                defaultCapacity: defaultPoolSize,
                maxSize: maxPoolSize
            );
        }

        private void Start()
        {
            // Setup the prototype object
            var prototypeRb = objPrefab.GetComponent<MovementAIRigidbody>();
            if (prototypeRb == null)
            {
                Debug.LogError("Prefab must have a MovementAIRigidbody component!", this);
                enabled = false;
                return;
            }

            prototypeRb.SetUp();
            isObj3D = prototypeRb.is3D;

            InitializeSpawnBoundaries();
            SpawnInitialObjects();
        }

        private void InitializeSpawnBoundaries()
        {
            float distAway = targetCamera.WorldToViewportPoint(Vector3.zero).z;
            bottomLeft = targetCamera.ViewportToWorldPoint(new Vector3(0, 0, distAway));
            Vector3 topRight = targetCamera.ViewportToWorldPoint(new Vector3(1, 1, distAway));
            widthHeight = topRight - bottomLeft;
        }

        private void SpawnInitialObjects()
        {
            for (int i = 0; i < numberOfObjects; i++)
            {
                const int maxAttempts = 10;
                bool success = false;

                for (int attempt = 0; attempt < maxAttempts && !success; attempt++)
                {
                    success = TryToCreateObject();
                }

                if (!success)
                {
                    Debug.LogWarning($"Failed to place object {i} after {maxAttempts} attempts", this);
                }
            }
        }

        private bool TryToCreateObject()
        {
            float size = Random.Range(objectSizeRange.x, objectSizeRange.y);
            float halfSize = size * 0.5f;

            Vector3 pos = GenerateRandomPosition(halfSize);
            
            if (!CanPlaceObject(halfSize, pos)) return false;

            var rb = objectPool.Get();
            rb.transform.position = pos;
            SetObjectSize(rb.transform, size);
            
            if (randomizeOrientation)
            {
                ApplyRandomRotation(rb.transform);
            }

            activeObjects.Add(rb);
            return true;
        }

        private Vector3 GenerateRandomPosition(float halfSize)
        {
            Vector3 pos = new();
            pos.x = bottomLeft.x + Random.Range(boundaryPadding + halfSize, widthHeight.x - boundaryPadding - halfSize);
            
            if (isObj3D)
            {
                pos.z = bottomLeft.z + Random.Range(boundaryPadding + halfSize, widthHeight.z - boundaryPadding - halfSize);
            }
            else
            {
                pos.y = bottomLeft.y + Random.Range(boundaryPadding + halfSize, widthHeight.y - boundaryPadding - halfSize);
            }
            
            return pos;
        }

        private void SetObjectSize(Transform transform, float size)
        {
            if (isObj3D)
            {
                transform.localScale = new Vector3(size, objPrefab.localScale.y, size);
            }
            else
            {
                transform.localScale = new Vector3(size, size, objPrefab.localScale.z);
            }
        }

        private void ApplyRandomRotation(Transform transform)
        {
            Vector3 euler = transform.eulerAngles;
            if (isObj3D)
            {
                euler.y = Random.Range(0f, 360f);
            }
            else
            {
                euler.z = Random.Range(0f, 360f);
            }
            transform.eulerAngles = euler;
        }

        private bool CanPlaceObject(float halfSize, Vector3 pos)
        {
            // Check against things to avoid
            foreach (var avoidObj in thingsToAvoid)
            {
                if (avoidObj == null) continue;
                
                float dist = Vector3.Distance(avoidObj.Position, pos);
                if (dist < halfSize + avoidObj.Radius)
                {
                    return false;
                }
            }

            // Check against existing objects
            foreach (var activeObj in activeObjects)
            {
                if (activeObj == null) continue;

                float dist = Vector3.Distance(activeObj.Position, pos);
                if (dist < activeObj.Radius + spaceBetweenObjects + halfSize)
                {
                    return false;
                }
            }

            return true;
        }

        #region Object Pool Methods
        private MovementAIRigidbody CreatePooledObject()
        {
            Transform instance = Instantiate(objPrefab, Vector3.zero, Quaternion.identity);
            instance.gameObject.SetActive(false);
            var rb = instance.GetComponent<MovementAIRigidbody>();
            rb.SetUp();
            return rb;
        }

        private void OnTakeFromPool(MovementAIRigidbody rb)
        {
            rb.gameObject.SetActive(true);
        }

        private void OnReturnToPool(MovementAIRigidbody rb)
        {
            rb.gameObject.SetActive(false);
        }

        private void OnDestroyPoolObject(MovementAIRigidbody rb)
        {
            Destroy(rb.gameObject);
        }

        public void DespawnObject(MovementAIRigidbody rb)
        {
            if (activeObjects.Remove(rb))
            {
                objectPool.Release(rb);
            }
        }
        #endregion

        #if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure reasonable values
            objectSizeRange.x = Mathf.Max(0.1f, objectSizeRange.x);
            objectSizeRange.y = Mathf.Max(objectSizeRange.x, objectSizeRange.y);
            numberOfObjects = Mathf.Max(0, numberOfObjects);
            boundaryPadding = Mathf.Max(0, boundaryPadding);
            spaceBetweenObjects = Mathf.Max(0, spaceBetweenObjects);
            defaultPoolSize = Mathf.Max(numberOfObjects, defaultPoolSize);
            maxPoolSize = Mathf.Max(defaultPoolSize, maxPoolSize);
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;

            Gizmos.color = Color.yellow;
            foreach (var obj in activeObjects)
            {
                if (obj != null)
                {
                    Gizmos.DrawWireSphere(obj.Position, obj.Radius + spaceBetweenObjects);
                }
            }
        }
        #endif
    }
}