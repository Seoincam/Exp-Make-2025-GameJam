using UnityEngine;

namespace Player
{
    public class PlayerCharacter : MonoBehaviour
    {
        [Header("Settings")] 
        [SerializeField] private float moveSpeed = 2f;
        
        [Header("References")]
        [SerializeField] private Rigidbody2D rb;
        
        private PlayerInputController _input;

        private Vector2 MoveInput => _input.Move * moveSpeed;

        private void Reset()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        private void Awake()
        {
            _input = new PlayerInputController();
            if (!rb) TryGetComponent(out rb);
        }

        private void FixedUpdate()
        {
            rb.linearVelocity = MoveInput;
        }
    }
}