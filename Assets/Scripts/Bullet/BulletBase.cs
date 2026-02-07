using UnityEngine;

namespace Combat.Shoot
{
    public abstract class BulletBase : MonoBehaviour
    {
        protected Transform Target;
        protected float Speed;
        protected float Damage;
        protected GameObject Owner;
        protected float MaxDistanceFromOwner;

        public virtual void Init(
            Transform target,
            float speed,
            float damage,
            GameObject owner,
            float maxDistanceFromOwner)
        {
            Target = target;
            Speed = speed;
            Damage = damage;
            Owner = owner;
            MaxDistanceFromOwner = maxDistanceFromOwner;
        }

        protected virtual void Update()
        {
            if (Owner)
            {
                float distanceToOwner = Vector2.Distance(transform.position, Owner.transform.position);
                if (distanceToOwner > MaxDistanceFromOwner)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            TickMovement(Time.deltaTime);
        }

        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            if (Owner && (other.gameObject == Owner || other.transform.IsChildOf(Owner.transform)))
            {
                return;
            }

            OnHit(other);
        }

        protected abstract void TickMovement(float deltaTime);
        protected abstract void OnHit(Collider2D other);
    }
}
