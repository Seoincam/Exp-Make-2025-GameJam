using Combat.Shoot;
using Shared.Stat;

namespace Player.State
{
    public class GarlicState : PlayerStateBase
    {
        private uint _speedEffectId;
        
        public GarlicState(IEntity entity) : base(entity)
        {
            StateType = PlayerState.Garlic;
        }

        public override void OnEnter()
        {
            var speedEffectSpec = Effect.CreateSpec(EffectType.GarlicSpeedBuff)
                .SetUnique()
                .AddHandler(new TemporaryModifierHandler(StatType.MoveSpeed, ModifierType.Multiplicative, 1.5f));
            _speedEffectId = Entity.EffectManager.AddEffect(speedEffectSpec);
        }

        public override void OnTick(float deltaTime)
        {
            
        }

        public override void OnExit()
        {
            Entity.EffectManager.SafeRemoveEffect(_speedEffectId, EffectType.GarlicSpeedBuff);
        }

        public override void OnDamage(DamageInfo damageInfo)
        {
            var spec = Effect.CreateSpec(EffectType.Damage)
                .SetOrder(EffectOrder.Early)
                .AddHandler(new InstantStatHandler(StatType.GarlicBullet, damageInfo.Damage));

            Entity.EffectManager.AddEffect(spec);
        }
    }
}