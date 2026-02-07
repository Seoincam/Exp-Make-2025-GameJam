using System.Collections.Generic;
using UnityEngine;

namespace Combat.Shoot
{
    public enum eFireMode
    {
        Normal,
    }

    [RequireComponent(typeof(ShootRangeDebugView))]
    public class ShootComponent : MonoBehaviour
    {
        [Header("Search")]
        [SerializeField] private float searchRadius = 10f;
        [SerializeField] private LayerMask enemyLayer;

        [Header("Bullet")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private float bulletSpeed = 15f;
        [SerializeField] private float bulletMaxDistance = 20f;
        [SerializeField] private float damage = 10f;

        public float SearchRadius => searchRadius;

        public bool IsCurrentBullet<TBullet>() where TBullet : BulletBase
        {
            return bulletPrefab && bulletPrefab.GetComponent<TBullet>() != null;
        }

        private void Awake()
        {
            if (!TryGetComponent<ShootRangeDebugView>(out _))
            {
                gameObject.AddComponent<ShootRangeDebugView>();
            }
        }

        private List<Transform> SearchEnemiesInRange()
        {
            var results = new List<Transform>();
            var hits = Physics2D.OverlapCircleAll(transform.position, searchRadius, enemyLayer);
            foreach (var hit in hits)
            {
                if (!hit || hit.transform == transform)
                {
                    continue;
                }

                if (!results.Contains(hit.transform))
                {
                    results.Add(hit.transform);
                }
            }

            if (results.Count > 0)
            {
                return results;
            }

            var monsters = FindObjectsOfType<MonsterController>();
            foreach (var monster in monsters)
            {
                if (!monster || !monster.gameObject.activeInHierarchy)
                {
                    continue;
                }

                int layerBit = 1 << monster.gameObject.layer;
                if ((enemyLayer.value & layerBit) == 0)
                {
                    continue;
                }

                float dist = Vector2.Distance(transform.position, monster.transform.position);
                if (dist <= searchRadius)
                {
                    results.Add(monster.transform);
                }
            }

            return results;
        }

        private Transform FindClosestEnemy()
        {
            var enemies = SearchEnemiesInRange();
            if (enemies.Count == 0)
            {
                return null;
            }

            Transform closest = null;
            float minDist = float.MaxValue;

            foreach (var enemy in enemies)
            {
                float dist = Vector2.Distance(transform.position, enemy.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = enemy.transform;
                }
            }

            return closest;
        }

        private GameObject FireAtClosestEnemy()
        {
            Transform target = FindClosestEnemy();
            if (target == null)
            {
                return null;
            }

            var bulletComp = BulletPool.Spawn(bulletPrefab, transform.position, Quaternion.identity);
            if (bulletComp)
            {
                bulletComp.Init(target, bulletSpeed, damage, gameObject, bulletMaxDistance);
            }

            return bulletComp ? bulletComp.gameObject : null;
        }

        public void Fire()
        {
            FireAtClosestEnemy();
        }
    }
}
