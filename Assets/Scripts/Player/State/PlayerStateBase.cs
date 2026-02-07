using Combat.Shoot;
using Shared.Stat;
using UnityEngine;

namespace Player.State
{
    public enum PlayerState
    {
        /// <summary>
        /// 멸치.
        /// </summary>
        Anchovy,
        
        /// <summary>
        /// 날치알.
        /// </summary>
        FlyingFishRoe,
        
        /// <summary>
        /// 소시지.
        /// </summary>
        Sausage,
        
        /// <summary>
        /// 마늘.
        /// </summary>
        Garlic,
        
        /// <summary>
        /// 고추참치.
        /// </summary>
        ChiliPepperAndTuna
    }
    
    public abstract class PlayerStateBase
    {
        protected IEntity Entity;

        protected Effect.EffectSpec DamageEffectSpec;
        
        public PlayerState StateType { get; protected set; }
        
        public bool EndRequested { get; protected set; }
        
        public PlayerStateBase(IEntity entity)
        {
            Entity = entity;
            
            DamageEffectSpec = Effect
                .CreateSpec(EffectType.Damage)
                .SetOrder(EffectOrder.Early);
        }

        public abstract void OnEnter();

        public abstract void OnTick(float deltaTime);

        public abstract void OnExit();

        public abstract void OnDamage(DamageInfo damageInfo);

        public virtual void OnStatChanged(Stat.StatChangedEventArgs args)
        {
            if (args.NewFinalValue <= 0.001f)
            {
                EndRequested = true;
            }
        }
    }
}