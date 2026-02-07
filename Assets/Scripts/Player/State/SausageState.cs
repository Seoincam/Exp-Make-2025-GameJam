using Combat.Shoot;
using Shared.Stat;

namespace Player.State
{
    public class SausageState : PlayerStateBase
    {
        public SausageState(IEntity entity) : base(entity)
        {
            StateType = PlayerState.Sausage;
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
                .AddHandler(new InstantStatHandler(StatType.SausageBullet, damageInfo.Damage));

            Entity.EffectManager.AddEffect(spec);
        }
    }
}