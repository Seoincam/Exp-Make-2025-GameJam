using Player.Control;
using Combat.Shoot;
using Shared.Stat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class PlayerInputController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private VirtualJoystick virtualJoystick;
        [SerializeField] private ShootComponent shootComponent;
        [SerializeField] private PlayerCharacter playerCharacter;

        [Header("Combat")]
        [SerializeField] private float fallbackFireInterval = 0.2f;
        [SerializeField] private float selfDamageOnFire = 1f;
        [SerializeField] private float inputCooldownSeconds = 1f;

        private PlayerInputSystem _inputSystem;
        private Vector2 _actionMove;
        private float _nextFireTime;
        
        public Vector2 Move { get; private set; }

        private void Awake()
        {
            _inputSystem = new PlayerInputSystem();

            _inputSystem.Player.Move.performed += OnMove;
            _inputSystem.Player.Move.canceled += OnMove;

            if (!virtualJoystick)
            {
                virtualJoystick = FindObjectOfType<VirtualJoystick>();
            }

            if (!shootComponent)
            {
                TryGetComponent(out shootComponent);
            }

            if (!playerCharacter)
            {
                TryGetComponent(out playerCharacter);
            }
        }

        private void OnEnable()
        {
            _inputSystem?.Player.Enable();
        }

        private void OnDisable()
        {
            _inputSystem?.Player.Disable();
        }

        private void OnDestroy()
        {
            if (_inputSystem == null)
            {
                return;
            }

            _inputSystem.Player.Move.performed -= OnMove;
            _inputSystem.Player.Move.canceled -= OnMove;
            _inputSystem.Dispose();
        }

        private void Update()
        {
            var joystickMove = virtualJoystick ? virtualJoystick.GetInputVector() : Vector2.zero;
            Move = joystickMove.sqrMagnitude > 0f ? joystickMove : _actionMove;

            HandleFireInput();
        }

        private void OnMove(InputAction.CallbackContext ctx)
        {
            _actionMove = ctx.ReadValue<Vector2>();
        }

        private void HandleFireInput()
        {
            if (Keyboard.current == null || shootComponent == null)
            {
                return;
            }

            bool isFireHeld = Keyboard.current.spaceKey.isPressed;
            if (!isFireHeld)
            {
                return;
            }

            float fireInterval = ResolveFireInterval();
            float inputCooldown = Mathf.Max(0f, inputCooldownSeconds);
            float effectiveCooldown = Mathf.Max(fireInterval, inputCooldown);
            float now = Time.time;

            if (now >= _nextFireTime)
            {
                if (shootComponent.TryFire())
                {
                    ApplySelfDamageOnFire();
                }
                _nextFireTime = now + effectiveCooldown;
            }
        }

        private float ResolveFireInterval()
        {
            if (playerCharacter != null && playerCharacter.Stat != null)
            {
                return Mathf.Max(0.01f, playerCharacter.Stat.GetFinalValue(StatType.FireInterval));
            }

            return Mathf.Max(0.01f, fallbackFireInterval);
        }

        private void ApplySelfDamageOnFire()
        {
            if (playerCharacter == null)
            {
                return;
            }

            float amount = Mathf.Max(0f, selfDamageOnFire);
            if (amount <= 0f)
            {
                return;
            }

            playerCharacter.TakeDamage(new DamageInfo(amount, playerCharacter.gameObject, EDamageType.Normal));
        }
    }
}
