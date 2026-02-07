using Combat.Shoot;
using Shared.Stat;
using UnityEngine;

namespace Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerCharacter : MonoBehaviour, IDamagable, IEntity
    {
        [Header("Settings")] 
        [SerializeField] private float moveSpeed = 2f;
        
        [Header("References")]
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private InitialStatConfig statConfig;

        [Header("States")] 
        [field: SerializeField] public Stat Stat { get; private set; }
        [field: SerializeField] public EffectManager EffectManager { get; private set; }
        
        private PlayerInputController _input;

        private Vector2 MoveInput => _input.Move * moveSpeed;

        private void Reset()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        private void Awake()
        {
            if (!_input) TryGetComponent(out _input);
            if (!rb) TryGetComponent(out rb);

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
            EffectManager.Tick(Time.fixedDeltaTime);
        }

        public void Damage(DamageInfo damageInfo)
        {
            var amount = damageInfo.Damage;

            if (Stat == null)
            {
                Debug.LogWarning($"{nameof(PlayerCharacter)} on {name} has no stat instance.");
                return;
            }

            if (amount <= 0f)
            {
                return;
            }

            var currentHp = Stat.GetBaseValue(StatType.Health);
            var nextHp = Mathf.Max(0f, currentHp - amount);
            Stat.SetBaseValue(StatType.Health, nextHp);
            Stat.ApplyPendingChanges();
        }
    }
}
