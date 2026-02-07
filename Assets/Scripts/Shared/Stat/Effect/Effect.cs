using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shared.Stat
{
    public enum EffectOrder
    {
        Early = -1,
        Default = 0,
        Late = 1
    }
    
    [Serializable]
    public class Effect
    {
        /// <summary>
        /// IsUnique인 효과가 존재할 경우, 중복 추가는 무시. 
        /// </summary>
        [field: SerializeField] public bool IsUnique { get; set; }
        [field: SerializeField] public EffectOrder Order { get; set; }
        [field: SerializeField] public EffectType Type { get; set; }
        
        [field: SerializeField] public uint InstanceID { get; set; }
        [field: SerializeField] public EffectContext Context { get; }
        
        [field: SerializeReference] private List<IEffectHandler> handlers = new();

        public bool End => Context.EndRequested;

        public Effect(EffectManager manager, Stat stat, List<IEffectHandler> handlers)
        {
            Context = new EffectContext(manager, stat);
            this.handlers.AddRange(handlers);
        }

        public void OnStart()
        {
            foreach (var handler in handlers)
            {
                handler.OnStart(Context);
            }
        }

        public void Tick(float deltaTime)
        {
            foreach (var handler in handlers)
            {
                handler.Tick(Context, deltaTime);
            }
        }

        public void OnEnd()
        {
            foreach (var handler in handlers)
            {
                handler.OnEnd(Context);
            }
        }
        
        public static EffectSpec CreateSpec(EffectType type) => new(type);
        
        /// <summary>
        /// 빌더 패턴을 사용해 이펙트 스펙을 구성하는 클래스.
        /// 실제 이펙트 인스턴스 생성은 <see cref="EffectManager"/>에서 수행됨.
        /// </summary>
        public class EffectSpec
        {
            public EffectType Type { get; private set; }
            public EffectOrder Order { get; private set; } = EffectOrder.Default;
            public bool IsUnique { get; private set; }
            public List<IEffectHandler> Handlers { get; } = new();
            
            public EffectSpec(EffectType type) => Type = type;
            
            public EffectSpec SetUnique(bool unique = true)
            {
                IsUnique = unique;
                return this;
            }

            public EffectSpec SetOrder(EffectOrder order)
            {
                Order = order;
                return this;
            }

            public EffectSpec AddHandler(IEffectHandler handler)
            {
                Handlers.Add(handler);
                return this;
            }
        }
    }
}