using Combat.Shoot;
using Shared.Stat;
using UnityEngine;

namespace Player.State
{
    public class FlyingFishRoeState : PlayerStateBase
    {
        public FlyingFishRoeState(IEntity entity) : base(entity)
        {
            StateType = PlayerState.FlyingFishRoe;
        }

        public override void OnEnter()
        {
        }

        public override void OnTick(float deltaTime)
        {
            
        }

        public override void OnExit()
        {
        }

        public override void OnDamage(DamageInfo damageInfo)
        {
            var spec = Effect.CreateSpec(EffectType.Damage)
                .SetOrder(EffectOrder.Early)
                .AddHandler(new InstantStatHandler(StatType.FlyingFishRoeBullet, damageInfo.Damage));
            
            Entity.EffectManager.AddEffect(spec);
        }

        public override void OnStatChanged(in Stat.StatChangedEventArgs args)
        {
            if (args.Type != StatType.FlyingFishRoeBullet) return;

            base.OnStatChanged(args);
        }
    }
}