using Shared.Stat;
using Combat.Shoot;
using UnityEngine;

namespace Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerCharacter : MonoBehaviour, IDamagable
    {
        [Header("Settings")] 
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float anchovyMoveSpeedMultiplier = 1.5f;
        
        [Header("References")]
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private InitialStatConfig statConfig;
        [SerializeField] private ShootComponent shootComponent;

        [Header("States")] 
        [SerializeField] private Stat stat;
        [SerializeField] private EffectManager effectManager;
        
        private PlayerInputController _input;

        private Vector2 MoveInput => _input.Move * GetCurrentMoveSpeed();

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

            stat = new Stat(statConfig);
            effectManager = new EffectManager(stat);

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
            effectManager.Tick(Time.fixedDeltaTime);
        }

        private float GetCurrentMoveSpeed()
        {
            float speed = moveSpeed;

            if (shootComponent && shootComponent.IsCurrentBullet<AnchovyBullet>())
            {
                speed *= anchovyMoveSpeedMultiplier;
            }

            return speed;
        }

        public void Damage(DamageInfo damageInfo)
        {
            var amount = damageInfo.Damage;

            if (stat == null)
            {
                Debug.LogWarning($"{nameof(PlayerCharacter)} on {name} has no stat instance.");
                return;
            }

            if (amount <= 0f)
            {
                return;
            }

            var currentHp = stat.GetBaseValue(StatType.Health);
            var nextHp = Mathf.Max(0f, currentHp - amount);
            stat.SetBaseValue(StatType.Health, nextHp);
            stat.ApplyPendingChanges();
        }
    }
}
