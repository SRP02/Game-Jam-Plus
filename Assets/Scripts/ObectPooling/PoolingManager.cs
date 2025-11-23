using System.Collections.Generic;
using UnityEngine;

namespace Race.Utility.PoolingSystem
{
    public class PoolingManager : MonoBehaviour
    {
        public static PoolingManager Instance { get; private set; }

        private readonly Dictionary<GameObject, Queue<GameObject>> _pools = new();
        private readonly Dictionary<GameObject, GameObject> _prefabLookup = new();
        // Track objects currently enqueued in pools (prevents duplicate enqueue)
        private readonly HashSet<GameObject> _enqueued = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null, bool worldPositionStays = true)
        {
            if (prefab == null) return null;

            if (!_pools.TryGetValue(prefab, out var queue))
            {
                CreatePool(prefab);
                queue = _pools[prefab];
            }

            GameObject obj;
            if (queue.Count > 0)
            {
                obj = queue.Dequeue();
                // Mark as not enqueued anymore
                _enqueued.Remove(obj);
            }
            else
            {
                obj = Instantiate(prefab);
                obj.transform.SetParent(transform, true);
                obj.transform.localScale = Vector3.one;
                _prefabLookup[obj] = prefab;
            }

            obj.transform.SetPositionAndRotation(position, rotation);

            if (parent != null && obj.transform.parent != parent)
                obj.transform.SetParent(parent, worldPositionStays);
            else if (parent == null && obj.transform.parent != transform)
                obj.transform.SetParent(transform, true);

            if (obj.transform.localScale != Vector3.one)
                obj.transform.localScale = Vector3.one;

            obj.SetActive(true);

            if (obj.TryGetComponent<IPoolObject>(out var poolable))
                poolable.OnSpawn();

            // Ensure mapping exists
            if (!_prefabLookup.ContainsKey(obj))
                _prefabLookup[obj] = prefab;

            return obj;
        }

        public void Despawn(GameObject obj)
        {
            if (obj == null) return;

            // If we don't manage this object, destroy it safely
            if (!_prefabLookup.TryGetValue(obj, out var prefab))
            {
                Debug.LogWarning($"Despawn called on unmanaged object: {obj.name} — destroying.");
                Destroy(obj);
                return;
            }

            // Prevent double-enqueue
            if (_enqueued.Contains(obj)) return;

            if (obj.TryGetComponent<IPoolObject>(out var poolable))
                poolable.OnDespawn();

            obj.SetActive(false);
            obj.transform.SetParent(transform, true);

            // Create pool if somehow missing
            if (!_pools.TryGetValue(prefab, out var queue))
            {
                CreatePool(prefab);
                queue = _pools[prefab];
            }

            queue.Enqueue(obj);
            _enqueued.Add(obj);
        }

        /// <summary>
        /// Permanently remove this instance from the pool system and destroy it.
        /// Use this when you intentionally want to tear down objects (editor cleanup or level unload).
        /// </summary>
        public void ReleaseAndDestroy(GameObject obj)
        {
            if (obj == null) return;

            if (_enqueued.Contains(obj))
                _enqueued.Remove(obj);

            if (_prefabLookup.TryGetValue(obj, out var prefab))
            {
                // If it's in that pool queue, remove it (cheap linear search)
                if (_pools.TryGetValue(prefab, out var q) && q.Count > 0)
                {
                    // Rebuild queue without the object (avoid keeping destroyed reference)
                    var temp = new Queue<GameObject>(q.Count);
                    while (q.Count > 0)
                    {
                        var x = q.Dequeue();
                        if (x != obj) temp.Enqueue(x);
                        else _prefabLookup.Remove(x);
                    }
                    _pools[prefab] = temp;
                }
                else
                {
                    _prefabLookup.Remove(obj);
                }
            }

            Destroy(obj);
        }

        public void CreatePool(GameObject prefab, int initialSize = 0)
        {
            if (prefab == null) return;
            if (_pools.ContainsKey(prefab)) return;

            var queue = new Queue<GameObject>();
            _pools[prefab] = queue;

            for (int i = 0; i < initialSize; i++)
            {
                var obj = Instantiate(prefab);
                obj.SetActive(false);
                obj.transform.SetParent(transform, true);
                obj.transform.localScale = Vector3.one;
                queue.Enqueue(obj);
                _prefabLookup[obj] = prefab;
                _enqueued.Add(obj); // mark created objects as enqueued
            }
        }

        public void DestroyPool(GameObject prefab)
        {
            if (prefab == null) return;
            if (!_pools.TryGetValue(prefab, out var queue)) return;

            while (queue.Count > 0)
            {
                var obj = queue.Dequeue();
                if (obj != null)
                {
                    _prefabLookup.Remove(obj);
                    _enqueued.Remove(obj);
                    Destroy(obj);
                }
            }
            _pools.Remove(prefab);
        }
    }
}
