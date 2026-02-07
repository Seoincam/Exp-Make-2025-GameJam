using System;
using Player.Control;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class PlayerInputController : MonoBehaviour
    {
        private PlayerInputSystem _inputSystem;
        
        public Vector2 Move { get; private set; }

        private void Awake()
        {
            _inputSystem = new PlayerInputSystem();
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
