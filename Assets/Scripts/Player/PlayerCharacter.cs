using Player.Stat;
using UnityEngine;

namespace Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerCharacter : MonoBehaviour
    {
        [Header("Settings")] 
        [SerializeField] private float moveSpeed = 2f;
        
        [Header("References")]
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private PlayerInitialStatConfig statConfig;

        [Header("States")] 
        [SerializeField] private PlayerStat stat;
        
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

            stat = new PlayerStat(statConfig);
        }

        private void FixedUpdate()
        {
            rb.linearVelocity = MoveInput;
        }
    }
}