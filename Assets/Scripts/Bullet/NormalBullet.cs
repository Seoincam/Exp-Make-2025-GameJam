using UnityEngine;

namespace Combat.Shoot
{
    public class NormalBullet : BulletBase
    {
        private Vector2 _initialDirection;

        public override void Init(
            Transform target,
            float damage,
            GameObject owner)
        {
            base.Init(target, damage, owner);

            if (Target)
            {
                Vector2 toTarget = (Vector2)Target.position - (Vector2)transform.position;
                _initialDirection = toTarget.sqrMagnitude > Mathf.Epsilon
                    ? toTarget.normalized
                    : Vector2.right;
            }
            else
            {
                _initialDirection = transform.right;
            }

            float angle = Mathf.Atan2(_initialDirection.y, _initialDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        protected override void TickMovement(float deltaTime)
        {
            transform.Translate(_initialDirection * (Speed * deltaTime), Space.World);
        }

        protected override void OnHit(Collider2D other)
        {
            IDamagable damageable = other.GetComponent<IDamagable>();
            if (damageable == null)
            {
                damageable = other.GetComponentInParent<IDamagable>();
            }

            if (damageable != null)
            {
                damageable.Damage(new DamageInfo(
                    Damage,
                    Owner,
                    EDamageType.Normal));

                ReturnToPool("HitDamageable");
                return;
            }

            if (other.GetComponentInParent<MonsterController>() != null)
            {
                ReturnToPool("HitMonsterWithoutIDamagable");
            }
        }
    }
}
