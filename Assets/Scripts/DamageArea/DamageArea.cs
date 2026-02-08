using System;
using Combat.Shoot;
using Shared.Stat;
using UnityEngine;
using UnityEngine.Pool;

namespace DamageArea
{
    /*
     * a. 5초마다 범위에 2 데미지, 패시브로 유지
     * b. 1초마다 범위에 1 데미지, 3초 유지
     * c. 0.5초마다 범위에 1.5 데미지, 5초 유지
     */
    public class DamageArea : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float interval;
        [SerializeField, Min(0f)] private float damage;
        [SerializeField] private float duration;

        [Space] 
        [SerializeField] private float radius;
        
        [Header("States")]
        [SerializeField] private float timer;
        [SerializeField] private float elapsed;

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            if (duration > 0f)
            {
                elapsed += dt;
                if (elapsed >= duration)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            timer += dt;
            if (timer >= interval)
            {
                timer -= interval;

                var entities = GetEntitiesInArea();
                if (entities.Length > 0)
                    ApplyDamage(entities);
            }
        }

        private IDamagable[] GetEntitiesInArea()
        {
            var hits = Physics2D.OverlapCircleAll(transform.position, radius);

            using var _ = ListPool<IDamagable>.Get(out var result);

            foreach (var hit in hits)
            {
                if (hit.TryGetComponent(out IDamagable enemy))
                    result.Add(enemy);
            }

            return result.ToArray();
        }

        private void ApplyDamage(IDamagable[] entities)
        {
            Debug.Log($"범위 데미지! 범위 내: {entities.Length}");
            foreach (var entity in entities)
            {
                entity.Damage(new DamageInfo(damage));
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}