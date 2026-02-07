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

#if UNITY_EDITOR
        private void OnValidate()
        {
            
        }
#endif

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

        private IEntity[] GetEntitiesInArea()
        {
            var hits = Physics2D.OverlapCircleAll(transform.position, radius);

            using var _ = ListPool<IEntity>.Get(out var result);

            foreach (var hit in hits)
            {
                if (hit.TryGetComponent(out IEntity entity))
                    result.Add(entity);
            }

            return result.ToArray();
        }

        private void ApplyDamage(IEntity[] entities)
        {
            var effectSpec = Effect.CreateSpec(EffectType.Damage)
                .AddHandler(new InstantStatHandler(StatType.Health, -damage));

            foreach (var entity in entities)
            {
                entity.EffectManager.AddEffect(effectSpec);
            }
        }
    }
}