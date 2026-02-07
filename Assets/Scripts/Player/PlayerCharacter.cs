using Combat.Shoot;
using Shared.Stat;
using Player.State;
using UnityEngine;

namespace Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerCharacter : MonoBehaviour, IDamagable, IEntity
    {
        [Header("References")]
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private InitialStatConfig statConfig;
        [SerializeField] private ShootComponent shootComponent;

        [Header("States")] 
        [field: SerializeField] public Stat Stat { get; private set; }
        [field: SerializeField] public EffectManager EffectManager { get; private set; }
        
        private PlayerInputController _input;

        private Vector2 MoveInput => _input.Move * Stat.GetFinalValue(StatType.MoveSpeed);

        private PlayerStateBase _currentState;

        private void Reset()
        {
            rb = GetComponent<Rigidbody2D>();
            shootComponent = GetComponent<ShootComponent>();
        }

        private void Awake()
        {
            if (!_input) TryGetComponent(out _input);
            if (!rb) TryGetComponent(out rb);
            if (!shootComponent) TryGetComponent(out shootComponent);

            Stat = new Stat(statConfig);
            EffectManager = new EffectManager(Stat);

            // 테스트
            // var effectSpec = Effect.CreateSpec(EffectType.Test)
            //     .SetUnique()
            //     .AddHandler(new LifeTimeHandler(5f))
            //     .AddHandler(new TemporaryModifierHandler(StatType.Health, ModifierType.Additive, 10f));
            // var id = effectManager.AddEffect(effectSpec);
        }

        private void FixedUpdate()
        {
            rb.linearVelocity = MoveInput;
            
            var deltaTime = Time.fixedDeltaTime;
            _currentState?.OnTick(deltaTime);
            EffectManager.Tick(deltaTime);
        }

        public void Damage(DamageInfo damageInfo)
        {
            if (Stat == null)
            {
                Debug.LogWarning($"{nameof(PlayerCharacter)} on {name} has no stat instance.");
                return;
            }

            var amount = damageInfo.Damage;
            if (amount <= 0f)
            {
                return;
            }

            // TODO: 병목된다면.. 이펙트 풀링
            var damageEffectSpec = Effect.CreateSpec(EffectType.Damage)
                .SetUnique(false)
                .AddHandler(new InstantStatHandler(StatType.Health, -amount));
            EffectManager.AddEffect(damageEffectSpec);
            
            _currentState?.OnDamage(damageInfo);
        }
    }
}
