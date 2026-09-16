using System;
using System.Collections.Generic;
using UnityEngine;

namespace Amanotes.Core
{
    [System.Serializable]
    public class PoolData
    {
        public string poolName;
        public GameObject prefab;
        public int prewarmCount = 10;
        public Transform poolParent;
    }

    public class ObjectPoolManager : SingletonMonoDontDestroy<ObjectPoolManager>
    {
        #region Serialized Fields

        [Header("Pool Settings")]
        public List<PoolData> poolDataList = new List<PoolData>();

        [Header("Return Position")]
        public Vector3 returnPosition = new Vector3(0, -1000, 0);
        public bool usePositionInsteadOfDeactivate = true;

        #endregion

        #region Private Fields

        public Transform poolContainer { get; set; }
        private Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();
        private Dictionary<string, GameObject> prefabDictionary = new Dictionary<string, GameObject>();

        #endregion

        #region Initialization

        protected override void Awake()
        {
            base.Awake();
            InitializePools();
        }

        private void InitializePools()
        {
            poolContainer = new GameObject("[POOL CONTAINER]").transform;

            foreach (var poolData in poolDataList)
            {
                if (poolData.prefab == null) continue;

                if (poolData.poolParent == null)
                {
                    GameObject parentObj = new GameObject($"Pool_{poolData.poolName}");
                    poolData.poolParent = parentObj.transform;
                    poolData.poolParent.SetParent(poolContainer);
                }

                Queue<GameObject> objectPool = new Queue<GameObject>();

                for (int i = 0; i < poolData.prewarmCount; i++)
                {
                    GameObject obj = UnityEngine.Object.Instantiate(poolData.prefab, poolData.poolParent);
                    obj.name = $"{poolData.poolName}_{i}";

                    if (usePositionInsteadOfDeactivate)
                        obj.transform.position = returnPosition;
                    else
                        obj.SetActive(false);

                    objectPool.Enqueue(obj);
                }

                poolDictionary[poolData.poolName] = objectPool;
                prefabDictionary[poolData.poolName] = poolData.prefab;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Get object from pool by enum
        /// </summary>
        public GameObject Get(PoolName poolName)
        {
            return Get(poolName.ToString());
        }

        /// <summary>
        /// Get object from pool by string
        /// </summary>
        public GameObject Get(string poolName)
        {
            Queue<GameObject> pool;
            if (!poolDictionary.TryGetValue(poolName, out pool))
            {
                SDebug.LogWarning($"[ObjectPoolManager] Pool '{poolName}' not found!");
                return null;
            }

            GameObject obj;

            if (pool.Count > 0)
            {
                obj = pool.Dequeue();
            }
            else
            {
                var poolData = poolDataList.Find(p => p.poolName == poolName);
                obj = UnityEngine.Object.Instantiate(prefabDictionary[poolName], poolData.poolParent);
                obj.name = $"{poolName}_dynamic";
            }

            if (usePositionInsteadOfDeactivate)
                obj.transform.position = Vector3.zero;
            else
                obj.SetActive(true);

            return obj;
        }

        /// <summary>
        /// Get object from pool at specific position and rotation
        /// </summary>
        public GameObject Get(PoolName poolName, Vector3 position, Quaternion rotation)
        {
            GameObject obj = Get(poolName);
            if (obj != null)
            {
                obj.transform.position = position;
                obj.transform.rotation = rotation;
            }
            return obj;
        }

        /// <summary>
        /// Return object to pool
        /// </summary>
        public void Return(GameObject obj, PoolName poolName)
        {
            Return(obj, poolName.ToString());
        }

        /// <summary>
        /// Return object to pool by string
        /// </summary>
        public void Return(GameObject obj, string poolName)
        {
            if (obj == null) return;

            Queue<GameObject> pool;
            if (!poolDictionary.TryGetValue(poolName, out pool))
            {
                SDebug.LogWarning($"[ObjectPoolManager] Pool '{poolName}' not found! Destroying object.");
                UnityEngine.Object.Destroy(obj);
                return;
            }

            obj.transform.position = returnPosition;
            obj.transform.rotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;

            var poolData = poolDataList.Find(p => p.poolName == poolName);
            obj.transform.SetParent(poolData.poolParent);

            if (!usePositionInsteadOfDeactivate)
                obj.SetActive(false);

            pool.Enqueue(obj);
        }

        /// <summary>
        /// Clear all pools
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var pool in poolDictionary.Values)
            {
                while (pool.Count > 0)
                {
                    var obj = pool.Dequeue();
                    if (obj != null) UnityEngine.Object.Destroy(obj);
                }
            }

            poolDictionary.Clear();
            prefabDictionary.Clear();
            poolDataList.Clear();

            if (poolContainer != null)
            {
                foreach (Transform child in poolContainer)
                    UnityEngine.Object.Destroy(child.gameObject);
            }

            SDebug.Log("[ObjectPoolManager] All pools cleared.");
        }

        /// <summary>
        /// Clear specific pool
        /// </summary>
        public void ClearPool(PoolName poolName)
        {
            ClearPool(poolName.ToString());
        }

        public void ClearPool(string poolName)
        {
            Queue<GameObject> pool;
            if (!poolDictionary.TryGetValue(poolName, out pool)) return;

            while (pool.Count > 0)
            {
                var obj = pool.Dequeue();
                if (obj != null) UnityEngine.Object.Destroy(obj);
            }

            poolDictionary.Remove(poolName);
            prefabDictionary.Remove(poolName);

            var poolData = poolDataList.Find(p => p.poolName == poolName);
            if (poolData != null && poolData.poolParent != null)
                UnityEngine.Object.Destroy(poolData.poolParent.gameObject);
        }

        /// <summary>
        /// Get available object count in pool
        /// </summary>
        public int GetPoolCount(PoolName poolName)
        {
            string name = poolName.ToString();
            Queue<GameObject> pool;
            if (poolDictionary.TryGetValue(name, out pool))
                return pool.Count;
            return 0;
        }

        #endregion
    }
}
