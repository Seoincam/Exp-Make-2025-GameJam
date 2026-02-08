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
        private const string NormalBulletSoPath = "Prefabs/BulletSO/Normal";

        [Header("References")]
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private InitialStatConfig statConfig;
        [SerializeField] private ShootComponent shootComponent;
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Transform body;
        [SerializeField] private PlayerFace face;

        [Header("States")] 
        [field: SerializeField] public Stat Stat { get; private set; }
        [field: SerializeField] public EffectManager EffectManager { get; private set; }
        public Transform Transform => transform;

        private PlayerInputController _input;
        
        public static PlayerCharacter Current { get; private set; }

        private Vector2 MoveInput => _input.Move * Stat.GetFinalValue(StatType.MoveSpeed);

        private PlayerStateBase _currentState;
        private GameObject _defaultBulletPrefab;

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
            _defaultBulletPrefab = shootComponent ? shootComponent.BulletPrefab : null;

            Stat.StatChanged += OnStatChanged;

            Current = this;
            
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
            animator.SetBool("Walking", MoveInput.magnitude > 0f);

            if (MoveInput.magnitude > 0f)
            {
                spriteRenderer.flipX = MoveInput.x < 0f;
                face.SpriteRenderer.flipX = MoveInput.x < 0f;
            }

            var deltaTime = Time.fixedDeltaTime;

            if (_currentState != null)
            {
                _currentState.OnTick(deltaTime);
                
                if (_currentState.EndRequested)
                {
                    EnterNormalMode();
                }
            }
            EffectManager.Tick(deltaTime);
        }

        private void OnDestroy()
        {
            DisposeCurrentState();
        }

        public void ChangeState(PlayerState nextStateType, bool forceReenter = false)
        {
            if (!forceReenter && _currentState != null && _currentState.StateType == nextStateType)
            {
                return;
            }

            var nextState = CreateState(nextStateType);
            if (nextState == null)
            {
                Debug.LogWarning($"Unsupported player state: {nextStateType}");
                return;
            }

            DisposeCurrentState();

            _currentState = nextState;
            _currentState.OnEnter();
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

            // Apply damage immediately instead of waiting for next FixedUpdate tick.
            EffectManager.Tick(0f);
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            Damage(damageInfo);
        }

        public void SetBulletPrefab(GameObject bulletPrefab)
        {
            if (!bulletPrefab)
            {
                return;
            }

            if (!shootComponent)
            {
                TryGetComponent(out shootComponent);
            }

            shootComponent?.SetBulletPrefab(bulletPrefab);
        }

        private PlayerStateBase CreateState(PlayerState stateType)
        {
            switch (stateType)
            {
                case PlayerState.Anchovy:
                    return new AnchovyState(this);
                case PlayerState.FlyingFishRoe:
                    return new FlyingFishRoeState(this);
                case PlayerState.Sausage:
                    return new SausageState(this);
                case PlayerState.Garlic:
                    return new GarlicState(this);
                case PlayerState.ChiliPepperAndTuna:
                    return new ChiliPepperAndTunaState(this);
                default:
                    return null;
            }
        }

        private void DisposeCurrentState()
        {
            if (_currentState == null)
            {
                return;
            }

            _currentState.OnExit();
            _currentState.Release();
            _currentState = null;
        }

        private void EnterNormalMode()
        {
            DisposeCurrentState();
            ApplyNormalBulletPrefab();
        }

        private void ApplyNormalBulletPrefab()
        {
            var normalSo = Resources.Load<global::BulletSO>(NormalBulletSoPath);
            if (normalSo && normalSo.BulletPrefab)
            {
                SetBulletPrefab(normalSo.BulletPrefab);
                return;
            }

            if (_defaultBulletPrefab)
            {
                SetBulletPrefab(_defaultBulletPrefab);
            }
        }

        private void OnStatChanged(in Stat.StatChangedEventArgs args)
        {
            if (args.Type == StatType.Health)
            {
                if (args.NewFinalValue >= 101f)
                {
                    body.localScale = Vector3.one * args.NewFinalValue / 100f;
                }
            }
        }
    }
}
