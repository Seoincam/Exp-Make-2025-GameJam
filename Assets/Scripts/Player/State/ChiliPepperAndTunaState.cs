using Combat.Shoot;
using Shared.Stat;

namespace Player.State
{
    public class ChiliPepperAndTunaState : PlayerStateBase
    {
        public ChiliPepperAndTunaState(IEntity entity) : base(entity)
        {
            StateType = PlayerState.ChiliPepperAndTuna;
        }

        public override void OnTick(float deltaTime)
        {
            
        }

        public override void OnDamage(DamageInfo damageInfo)
        {
            var spec = Effect.CreateSpec(EffectType.Damage)
                .SetOrder(EffectOrder.Early)
                .AddHandler(new InstantStatHandler(StatType.ChiliPepperAndTunaBullet, damageInfo.Damage));
            
            Entity.EffectManager.AddEffect(spec);
        }
    }
}