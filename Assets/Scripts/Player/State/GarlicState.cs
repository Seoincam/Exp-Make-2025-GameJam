using Combat.Shoot;
using Shared.Stat;
using UnityEngine;

namespace Player.State
{
    public class GarlicState : PlayerStateBase
    {
        
        public GarlicState(IEntity entity) : base(entity)
        {
            StateType = PlayerState.Garlic;
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
                .AddHandler(new InstantStatHandler(StatType.GarlicBullet, damageInfo.Damage));

            Entity.EffectManager.AddEffect(spec);
        }

        public override void OnStatChanged(in Stat.StatChangedEventArgs args)
        {
            if (args.Type != StatType.GarlicBullet) return;

            base.OnStatChanged(args);
        }
    }
}