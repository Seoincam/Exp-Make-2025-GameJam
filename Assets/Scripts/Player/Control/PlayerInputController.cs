using Player.Control;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class PlayerInputController
    {
        private readonly PlayerInputSystem _inputSystem = new();
        
        public Vector2 Move { get; private set; }

        public PlayerInputController()
        {
            _inputSystem.Player.Enable();
            
            _inputSystem.Player.Move.performed += OnMove;
            _inputSystem.Player.Move.canceled += OnMove;
        }

        private void OnMove(InputAction.CallbackContext ctx)
        {
            Move = ctx.ReadValue<Vector2>();
        }
    }

}
