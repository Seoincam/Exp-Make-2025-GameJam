using System;
using UnityEngine;

namespace Shared.Stat
{
    /// <summary>
    /// 일시적인 스탯의 증감을 안전하게 수행함.
    /// 이펙트가 종료될 시 일시적인 증감이 해제됨.
    /// </summary>
    [Serializable]
    public class TemporaryModifierHandler : IEffectHandler
    {
        [field: SerializeField] public StatType StatType { get; private set; }
        
        [field: SerializeField] public ModifierType Type { get; private set; }
        [SerializeField] private float value;

        private uint _modifierInstanceId;
        
        public TemporaryModifierHandler(StatType statType, ModifierType type, float value)
        {
            StatType = statType;
            Type = type;
            this.value = value;
        }
        
        public void OnStart(EffectContext context)
        {
            var modifier = new TemporaryModifier(StatType, Type, value);
            _modifierInstanceId = context.Manager.AddModifier(modifier);
            Debug.Log(_modifierInstanceId);
        }

        public void Tick(EffectContext context, float deltaTime)
        {
        }

        public void OnEnd(EffectContext context)
        {
            context.Manager.RemoveModifier(_modifierInstanceId);
        }
    }
    
    public enum ModifierType
    {
        Additive,
        Multiplicative,
        Override,
    }
    
    [Serializable]
    public class TemporaryModifier
    {
        [field: SerializeField] public StatType StatType { get; private set; }
        
        [field: SerializeField] public ModifierType Type { get; private set; }
        [SerializeField] private float value;
        
        public TemporaryModifier(StatType statType, ModifierType type, float value)
        {
            StatType = statType;
            Type = type;
            this.value = value;
        }

        public float Apply(float baseValue, float modifiedValue)
        {
            switch (Type)
            {
                case ModifierType.Additive: return modifiedValue + value;
                case ModifierType.Multiplicative: return modifiedValue * value;
                case ModifierType.Override: return value;
            }

            return modifiedValue;
        }
    }
}