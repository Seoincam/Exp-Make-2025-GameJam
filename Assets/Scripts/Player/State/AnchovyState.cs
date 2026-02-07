using Combat.Shoot;
using Shared.Stat;
using UnityEngine;

namespace Player.State
{
    /// <summary>
    /// 멸치 탄알을 소유한 상태.
    /// </summary>
    public class AnchovyState : PlayerStateBase
    {
        private uint _speedEffectId;
        
        public AnchovyState(IEntity entity) : base(entity)
        {
            StateType = PlayerState.Anchovy;
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
                .AddHandler(new InstantStatHandler(StatType.AnchovyBullet, damageInfo.Damage));
            Entity.EffectManager.AddEffect(spec);
        }

        public override void OnStatChanged(Stat.StatChangedEventArgs args)
        {
            if (args.Type != StatType.AnchovyBullet) return;

            base.OnStatChanged(args);
        }
    }
}