using System.Collections.Generic;
using Player;
using Shared.Stat;
using UnityEngine;

namespace Combat.Shoot
{
    /// <summary>
    /// 범위 효과 적용 컴포넌트.
    /// 일단 마늘 효과만 적용함.
    /// </summary>
    public class AreaEffectApplier : MonoBehaviour
    {
        private readonly Dictionary<IEntity, uint> _effectIdCache = new();

        private Effect.EffectSpec _spec;

        private void Awake()
        {
            _spec = Effect.CreateSpec(EffectType.GarlicAreaDamage)
                .SetUnique()
                .AddHandler(new PeriodicStatHandler(StatType.Health, 0.3f, 0.5f));
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.gameObject.TryGetComponent(out IEntity entity)) return;
            if (entity is PlayerCharacter) return;
            if (_effectIdCache.ContainsKey(entity)) return;

            var id = entity.EffectManager.AddEffect(_spec);
            _effectIdCache.Add(entity, id);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.gameObject.TryGetComponent(out IEntity entity)) return;
            if (entity is PlayerCharacter) return;

            if (_effectIdCache.TryGetValue(entity, out var id))
            {
                entity.EffectManager.SafeRemoveEffect(id, EffectType.GarlicAreaDamage);
                _effectIdCache.Remove(entity);
            }
        }
    }
}