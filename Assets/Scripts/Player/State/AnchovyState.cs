using Combat.Shoot;
using Shared.Stat;

namespace Player.State
{
    /// <summary>
    /// 멸치 탄알을 소유한 상태.
    /// </summary>
    public class AnchovyState : PlayerStateBase
    {
        
        public AnchovyState(IEntity entity) : base(entity)
        {
            StateType = PlayerState.Anchovy;
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
                .AddHandler(new InstantStatHandler(StatType.AnchovyBullet, damageInfo.Damage));
            Entity.EffectManager.AddEffect(spec);
        }
    }
}