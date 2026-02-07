using System;
using UnityEngine;

namespace Shared.Stat
{
    [Serializable]
    public class PeriodicStatHandler : IEffectHandler
    {
        [SerializeField] private StatType statType;
        [SerializeField] private float interval;
        [SerializeField] private float value;

        private float _timer;

        public PeriodicStatHandler(StatType statType, float interval, float value)
        {
            this.statType = statType;
            this.interval = interval;
            this.value = value;
        }
        
        public void OnStart(EffectContext context)
        {
        }

        public void Tick(EffectContext context, float deltaTime)
        {
            if (context.EndRequested) return;
            
            _timer += deltaTime;
            if (_timer >= interval)
            {
                _timer -= interval;
                context.Stat.ModifyBaseValue(statType, value);
            }
        }

        public void OnEnd(EffectContext context)
        {
        }
    }
}