using System.Collections.Generic;
using UnityEngine;

namespace Combat.Shoot
{
    public static class BulletPool
    {
        private sealed class Pool
        {
            public readonly Queue<BulletBase> Inactive = new();
            public readonly Transform Root;

            public Pool(GameObject prefab)
            {
                var rootObject = new GameObject($"{prefab.name}_Pool");
                Root = rootObject.transform;
                Object.DontDestroyOnLoad(rootObject);
            }
        }

        private static readonly Dictionary<int, Pool> Pools = new();
        private static readonly Dictionary<int, int> InstanceToPoolKey = new();

        public static BulletBase Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (!prefab)
            {
                return null;
            }

            int poolKey = prefab.GetInstanceID();
            if (!Pools.TryGetValue(poolKey, out var pool))
            {
                pool = new Pool(prefab);
                Pools.Add(poolKey, pool);
            }

            BulletBase bullet = null;
            while (pool.Inactive.Count > 0 && !bullet)
            {
                bullet = pool.Inactive.Dequeue();
            }

            if (!bullet)
            {
                var instance = Object.Instantiate(prefab, position, rotation);
                if (!instance.TryGetComponent(out bullet))
                {
                    Object.Destroy(instance);
                    return null;
                }
            }
            else
            {
                bullet.transform.SetParent(null);
                bullet.transform.SetPositionAndRotation(position, rotation);
                bullet.gameObject.SetActive(true);
            }

            InstanceToPoolKey[bullet.GetInstanceID()] = poolKey;
            return bullet;
        }

        public static void Return(BulletBase bullet)
        {
            if (!bullet)
            {
                return;
            }

            int instanceId = bullet.GetInstanceID();
            if (!InstanceToPoolKey.TryGetValue(instanceId, out var poolKey) || !Pools.TryGetValue(poolKey, out var pool))
            {
                Object.Destroy(bullet.gameObject);
                return;
            }

            bullet.transform.SetParent(pool.Root, false);
            bullet.gameObject.SetActive(false);
            pool.Inactive.Enqueue(bullet);
        }
    }
}
