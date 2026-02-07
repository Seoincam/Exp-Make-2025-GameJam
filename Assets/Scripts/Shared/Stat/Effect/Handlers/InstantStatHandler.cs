using System;
using UnityEngine;

namespace Shared.Stat
{
    /// <summary>
    /// 스탯의 증감을 수행함.
    /// </summary>
    [Serializable]
    public class InstantStatHandler : IEffectHandler
    {
        [SerializeField] private StatType statType;
        [SerializeField] private float delta;
        [SerializeField] private bool requestEnd;

        public InstantStatHandler(StatType statType, float delta, bool requestEnd = true)
        {
            this.statType = statType;
            this.delta = delta;
            this.requestEnd = requestEnd;
        }
        
        public void OnStart(EffectContext context)
        {
            context.Stat.ModifyBaseValue(statType, delta);
            
            if (requestEnd)
            {
                context.RequestEnd();
            }
        }

        public void Tick(EffectContext context, float deltaTime)
        {
        }

        public void OnEnd(EffectContext context)
        {
        }
    }
}