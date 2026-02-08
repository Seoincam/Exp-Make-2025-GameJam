using Combat.Shoot;
using Shared.Stat;
using UnityEngine;

namespace Player.State
{
    public class GarlicState : PlayerStateBase
    {
        private GameObject _garlicPassiveArea;
        
        public GarlicState(IEntity entity) : base(entity)
        {
            StateType = PlayerState.Garlic;
        }

        public override void OnEnter()
        {
            var prefab = Resources.Load<GameObject>("Prefabs/Garlic Passive Area");
            _garlicPassiveArea = Object.Instantiate(prefab, Entity.Transform);
        }

        public override void OnTick(float deltaTime)
        {
        }

        public override void OnExit()
        {
            Object.Destroy(_garlicPassiveArea);
        }

        public override void OnDamage(DamageInfo damageInfo)
        {
            var spec = Effect.CreateSpec(EffectType.Damage)
                .SetOrder(EffectOrder.Early)
                .AddHandler(new InstantStatHandler(StatType.GarlicBullet, -damageInfo.Damage));

            Entity.EffectManager.AddEffect(spec);
        }

        public override void OnStatChanged(in Stat.StatChangedEventArgs args)
        {
            if (args.Type != StatType.GarlicBullet) return;

            base.OnStatChanged(args);
        }
    }
}
