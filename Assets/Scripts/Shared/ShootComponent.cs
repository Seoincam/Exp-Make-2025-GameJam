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
        [SerializeField] private float bulletLifetime = 3f;
        [SerializeField] private float damage = 10f;

        public float SearchRadius => searchRadius;

        private void Awake()
        {
            if (!TryGetComponent<ShootRangeDebugView>(out _))
            {
                gameObject.AddComponent<ShootRangeDebugView>();
            }
        }

        private List<Collider2D> SearchEnemiesInRange()
        {
            var hits = Physics2D.OverlapCircleAll(transform.position, searchRadius, enemyLayer);
            return new List<Collider2D>(hits);
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
                Debug.Log("ShootComponent: no target in range.");
                return null;
            }

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

            Debug.Log($"ShootComponent: fired bullet to {target.name}");

            return bullet;
        }

        public void Fire()
        {
            Debug.Log("ShootComponent: fire requested.");
            FireAtClosestEnemy();
        }
    }
}
