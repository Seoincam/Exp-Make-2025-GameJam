using Player.Control;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class PlayerInputController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private VirtualJoystick virtualJoystick;

        private PlayerInputSystem _inputSystem;
        private Vector2 _actionMove;
        
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
        }

        private void OnMove(InputAction.CallbackContext ctx)
        {
            _actionMove = ctx.ReadValue<Vector2>();
        }
    }
}
