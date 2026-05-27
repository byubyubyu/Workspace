using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Workspace.Patterns
{
    public class MinionPool : MonoBehaviour
    {
        [SerializeField] private MinionPoolConfig config;

        private Dictionary<GameObject, ObjectPool<GameObject>> pools
            = new Dictionary<GameObject, ObjectPool<GameObject>>();

        void Awake()
        {
            Core.ServiceLocator.Register<MinionPool>(this);
            InitializePools();
        }

        private void InitializePools()
        {
            foreach (MinionPoolConfig.PoolEntry entry in config.Entries)
            {
                if (entry.prefab == null) continue;

                GameObject prefab = entry.prefab;
                int initialSize = entry.initialSize;
                int maxSize = entry.maxSize;

                ObjectPool<GameObject> pool = new ObjectPool<GameObject>(
                    createFunc: () => Instantiate(prefab),
                    actionOnGet: obj => obj.SetActive(true),
                    actionOnRelease: obj => obj.SetActive(false),
                    actionOnDestroy: obj => Destroy(obj),
                    collectionCheck: true,
                    defaultCapacity: initialSize,
                    maxSize: maxSize
                );

                pools[prefab] = pool;

                List<GameObject> prewarmed = new List<GameObject>();
                for (int i = 0; i < initialSize; i++)
                    prewarmed.Add(pool.Get());
                foreach (GameObject obj in prewarmed)
                    pool.Release(obj);
            }
        }

        public GameObject Get(GameObject prefab)
        {
            if (!pools.ContainsKey(prefab))
            {
                Debug.LogError($"[MinionPool] {prefab.name}のPoolが存在しません");
                return null;
            }
            return pools[prefab].Get();
        }

        public void Return(GameObject prefab, GameObject obj)
        {
            if (!pools.ContainsKey(prefab))
            {
                Debug.LogError($"[MinionPool] {prefab.name}のPoolが存在しません");
                Destroy(obj);
                return;
            }
            pools[prefab].Release(obj);
        }

        void OnDestroy()
        {
            Core.ServiceLocator.Unregister<MinionPool>();
        }
    }
}