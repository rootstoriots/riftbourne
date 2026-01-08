using UnityEngine;
using System.Collections.Generic;

namespace Riftbourne.Core
{
    /// <summary>
    /// Generic object pool for reusing GameObjects.
    /// Reduces allocation and garbage collection overhead.
    /// </summary>
    public class ObjectPool : MonoBehaviour
    {
        [Header("Pool Settings")]
        [SerializeField] private GameObject prefab;
        [SerializeField] private int initialSize = 5;
        [SerializeField] private int maxSize = 20;
        [SerializeField] private bool expandPool = true;

        private Queue<GameObject> pool = new Queue<GameObject>();
        private List<GameObject> activeObjects = new List<GameObject>();

        private void Awake()
        {
            if (prefab == null)
            {
                Debug.LogError("ObjectPool: Prefab is not assigned!");
                return;
            }

            // Pre-populate pool
            for (int i = 0; i < initialSize; i++)
            {
                GameObject obj = CreatePooledObject();
                ReturnToPool(obj);
            }
        }

        /// <summary>
        /// Get an object from the pool. Creates a new one if pool is empty and expansion is allowed.
        /// </summary>
        public GameObject Get()
        {
            GameObject obj;

            if (pool.Count > 0)
            {
                obj = pool.Dequeue();
            }
            else if (expandPool)
            {
                obj = CreatePooledObject();
            }
            else
            {
                Debug.LogWarning("ObjectPool: Pool is empty and expansion is disabled!");
                return null;
            }

            obj.SetActive(true);
            activeObjects.Add(obj);
            return obj;
        }

        /// <summary>
        /// Return an object to the pool.
        /// </summary>
        public void Return(GameObject obj)
        {
            if (obj == null) return;

            if (!activeObjects.Contains(obj))
            {
                Debug.LogWarning("ObjectPool: Attempted to return object that wasn't from this pool!");
                return;
            }

            activeObjects.Remove(obj);
            ReturnToPool(obj);
        }

        /// <summary>
        /// Return all active objects to the pool.
        /// </summary>
        public void ReturnAll()
        {
            while (activeObjects.Count > 0)
            {
                Return(activeObjects[0]);
            }
        }

        private GameObject CreatePooledObject()
        {
            GameObject obj = Instantiate(prefab);
            obj.name = $"{prefab.name}_Pooled";
            obj.SetActive(false);
            return obj;
        }

        private void ReturnToPool(GameObject obj)
        {
            obj.SetActive(false);
            obj.transform.SetParent(transform);
            
            // Check pool size limit
            if (pool.Count >= maxSize)
            {
                Destroy(obj);
            }
            else
            {
                pool.Enqueue(obj);
            }
        }

        /// <summary>
        /// Get the number of objects currently in the pool.
        /// </summary>
        public int PoolSize => pool.Count;

        /// <summary>
        /// Get the number of objects currently active.
        /// </summary>
        public int ActiveCount => activeObjects.Count;
    }
}

