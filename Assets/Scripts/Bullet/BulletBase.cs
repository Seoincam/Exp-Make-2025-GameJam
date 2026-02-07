using UnityEngine;

namespace Combat.Shoot
{
    [RequireComponent(typeof(CircleCollider2D), typeof(Rigidbody2D))]
    public abstract class BulletBase : MonoBehaviour
    {
        [Header("Bullet Runtime")]
        [SerializeField, Min(0.01f)] private float defaultSpeed = 15f;
        [SerializeField, Min(0.1f)] private float defaultMaxDistanceFromOwner = 20f;

        private CircleCollider2D _circleCollider2D;
        private Rigidbody2D _rigidbody2D;

        protected Transform Target;
        protected float Speed;
        protected float Damage;
        protected GameObject Owner;
        protected float MaxDistanceFromOwner;

        protected virtual void Reset()
        {
            EnsurePhysicsSetup();
        }

        protected virtual void OnValidate()
        {
            EnsurePhysicsSetup();
        }

        protected virtual void Awake()
        {
            EnsurePhysicsSetup();
        }

        public virtual void Init(
            Transform target,
            float damage,
            GameObject owner)
        {
            Target = target;
            Speed = defaultSpeed;
            Damage = damage;
            Owner = owner;
            MaxDistanceFromOwner = defaultMaxDistanceFromOwner;
        }

        protected virtual void FixedUpdate()
        {
            if (Owner)
            {
                float distanceToOwner = Vector2.Distance(transform.position, Owner.transform.position);
                if (distanceToOwner > MaxDistanceFromOwner)
                {
                    ReturnToPool("DistanceLimit");
                    return;
                }
            }

            TickMovement(Time.fixedDeltaTime);
        }

        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            if (Owner && (other.gameObject == Owner || other.transform.IsChildOf(Owner.transform)))
            {
                return;
            }

            OnHit(other);
        }

        public void ReturnToPool(string reason = null)
        {
            BulletPool.Return(this);
        }

        private void EnsurePhysicsSetup()
        {
            _circleCollider2D = GetComponent<CircleCollider2D>();
            if (!_circleCollider2D)
            {
                _circleCollider2D = gameObject.AddComponent<CircleCollider2D>();
            }
            _circleCollider2D.isTrigger = true;

            _rigidbody2D = GetComponent<Rigidbody2D>();
            if (!_rigidbody2D)
            {
                _rigidbody2D = gameObject.AddComponent<Rigidbody2D>();
            }
            _rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
            _rigidbody2D.gravityScale = 0f;
            _rigidbody2D.simulated = true;
            _rigidbody2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        protected abstract void TickMovement(float deltaTime);
        protected abstract void OnHit(Collider2D other);
    }
}
