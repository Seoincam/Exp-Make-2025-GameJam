using System.Collections.Generic;
using UnityEngine;

namespace Combat.Shoot
{
    public class ShootComponent : MonoBehaviour
    {
        [Header("탐색")]
        [SerializeField] private float searchRadius = 10f;
        [SerializeField] private LayerMask enemyLayer;

        [Header("탄")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private float bulletSpeed = 15f;
        [SerializeField] private float bulletLifetime = 3f;
        [SerializeField] private float damage = 10f;

        /// <summary>
        /// 1. 일정 범위 안의 적을 모두 탐색하여 반환한다.
        /// </summary>
        public List<Collider2D> SearchEnemiesInRange()
        {
            var results = new List<Collider2D>();
            var hits = Physics2D.OverlapCircleAll(transform.position, searchRadius, enemyLayer);
            results.AddRange(hits);
            return results;
        }

        /// <summary>
        /// 2. 범위 안의 적 중 가장 가까운 적을 찾아 반환한다.
        ///    적이 없으면 null.
        /// </summary>
        public Transform FindClosestEnemy()
        {
            var enemies = SearchEnemiesInRange();
            if (enemies.Count == 0) return null;

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

        /// <summary>
        /// 3. 가장 가까운 적을 향해 탄을 생성한다.
        ///    타깃이 없으면 null 반환.
        /// </summary>
        public GameObject FireAtClosestEnemy()
        {
            Transform target = FindClosestEnemy();
            if (target == null) return null;

            Vector2 direction = ((Vector2)target.position - (Vector2)transform.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            GameObject bullet = Instantiate(
                bulletPrefab,
                transform.position,
                Quaternion.Euler(0, 0, angle)
            );

            if (bullet.TryGetComponent<Bullet>(out var bulletComp))
            {
                bulletComp.Init(direction, bulletSpeed, bulletLifetime, damage, gameObject);
            }

            return bullet;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, searchRadius);
        }
    }
}