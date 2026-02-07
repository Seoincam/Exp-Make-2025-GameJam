using Combat.Shoot;
using Shared.Stat;

namespace Player.State
{
    public class FlyingFishRoeState : PlayerStateBase
    {
        
        public FlyingFishRoeState(IEntity entity) : base(entity)
        {
            StateType = PlayerState.FlyingFishRoe;
        }

        public override void OnTick(float deltaTime)
        {
            
        }

        public override void OnDamage(DamageInfo damageInfo)
        {
            var spec = Effect.CreateSpec(EffectType.Damage)
                .SetOrder(EffectOrder.Early)
                .AddHandler(new InstantStatHandler(StatType.FlyingFishRoeBullet, damageInfo.Damage));
            
            Entity.EffectManager.AddEffect(spec);
        }
    }
}