using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Shared.Stat
{
    [Serializable]
    public class EffectManager
    {
        [Header("Sorted")]
        [SerializeField] private List<Effect> sortedEffects = new();
        private Dictionary<StatType, List<TemporaryModifier>> _sortedModifiers = new();
        
        private Stat _stat;
        
        private Dictionary<uint, Effect> _effects = new();
        private Dictionary<uint, TemporaryModifier> _modifiers = new();
        
        private Dictionary<EffectType, List<Effect>> _effectTypeCache = new();
        
        private uint _nextInstanceId = 1;

        private bool _needSortEffects = true;
        private bool _needSortModifiers = true;
        
        public EffectManager(Stat stat)
        {
            _stat = stat;
        }
        
        public void Tick(float deltaTime)
        {
            if (_needSortEffects)
            {
                SortEffects();
            }
            if (_needSortModifiers)
            {
                SortModifiers();
            }

            using var _ = ListPool<uint>.Get(out var toRemove);
            
            foreach (var effect in sortedEffects)
            {
                effect.Tick(deltaTime);

                if (effect.End)
                {
                    toRemove.Add(effect.InstanceID);
                }
            }
            
            foreach (var instanceId in toRemove)
            {
                RemoveEffect(instanceId);
            }
            
            ApplyModifiers();
            
            // TODO: 필요하다면 변경된거 이벤트 발행
        }

        public uint AddEffect(Effect.EffectSpec effectSpec)
        {
            if (GetEffectCount(effectSpec.Type) > 0)
            {
                Debug.Log("Unique한 이벤트가 중복 추가 시도됐습니다. 무시.");
                return 0;
            }
            
            var instanceId = _nextInstanceId++;
            var effect = CreateEffectInstance(effectSpec, instanceId);
            
            sortedEffects.Add(effect);
            _effects.Add(instanceId, effect);
            if (!_effectTypeCache.TryGetValue(effect.Type, out var list))
            {
                list = new List<Effect>();
            }
            list.Add(effect);

            effect.OnStart();
            _needSortEffects = true;
            return instanceId;
        }

        public uint AddModifier(TemporaryModifier modifier)
        {
            var instanceId = _nextInstanceId++;
            _modifiers.Add(instanceId, modifier);
            
            _needSortModifiers = true;
            return instanceId;
        }
        
        /// <summary>
        /// 안전한 제거.
        /// 인스턴스 Id에 해당하는 효과의 태그가 일치할 경우만 제거합니다.
        /// </summary>
        public void SafeRemoveEffect(uint instanceId, EffectType effectType)
        {
            if (_effects.TryGetValue(instanceId, out var effect))
            {
                if (effect.Type == effectType)
                {
                    RemoveEffect(instanceId);
                }
            }
        }

        public void RemoveModifier(uint instanceId)
        {
            _modifiers.Remove(instanceId);
            _needSortModifiers = true;
        }


        public int GetEffectCount(EffectType type)
        {
            if (_effectTypeCache.TryGetValue(type, out var list))
            {
                return list.Count;
            }

            return 0;
        }

        private void RemoveEffect(uint instanceId)
        {
            if (_effects.TryGetValue(instanceId, out var effect))
            {
                effect.OnEnd();
            }
            
            _effects.Remove(instanceId);
            _needSortEffects = true;
        }
        
        private void ApplyModifiers()
        {
            foreach (var (statType, list) in _sortedModifiers)
            {
                var baseValue = _stat.GetBaseValue(statType);
                var finalValue = _stat.GetFinalValue(statType);
                
                foreach (var modifier in list)
                { 
                    finalValue = modifier.Apply(baseValue, finalValue);
                }
                
                _stat.SetFinalValue(statType, finalValue);
            }
        }

        private void SortEffects()
        {
            sortedEffects.Clear();
            sortedEffects.AddRange(_effects.Values);
            sortedEffects.Sort((a, b) => a.Order.CompareTo(b.Order));

            _needSortEffects = false;
        }

        private void SortModifiers()
        {
            _sortedModifiers.Clear();
            
            foreach (var modifier in _modifiers.Values)
            {
                if (!_sortedModifiers.TryGetValue(modifier.StatType, out var list))
                {
                    list = new List<TemporaryModifier>();
                }
                list.Add(modifier);
            }

            foreach (var list in _sortedModifiers.Values)
            {
                list.Sort((a, b) => a.Type.CompareTo(b.Type));
            }

            _needSortModifiers = false;
        }

        /// <summary>
        /// <c>EffectSpec</c>으로 Effect 인스턴스를 생성해 반환함.
        /// </summary>
        private Effect CreateEffectInstance(Effect.EffectSpec effectSpec, uint instanceId)
        {
            var effectInstance = new Effect(this, _stat, effectSpec.Handlers)
            {
                IsUnique = effectSpec.IsUnique,
                Order = effectSpec.Order,
                Type = effectSpec.Type,
                InstanceID = instanceId
            };

            return effectInstance;
        }
    }
}