using System.Collections.Generic;
using UnityEngine;

namespace Combat.Shoot
{
    public class AnchovyBullet : BulletBase
    {
        [Header("Anchovy")]
        [SerializeField, Range(0.01f, 1f)] private float initialDamageMultiplier = 0.75f;
        [SerializeField, Range(0.01f, 1f)] private float damageMultiplierPerPierce = 0.85f;
        [SerializeField, Min(0f)] private float minDamageToKeepPiercing = 0.1f;

        private readonly HashSet<int> _damagedTargetIds = new();
        private Vector2 _initialDirection;
        private float _currentDamage;

        public override void Init(
            Transform target,
            float speed,
            float damage,
            GameObject owner,
            float maxDistanceFromOwner)
        {
            base.Init(target, speed, damage, owner, maxDistanceFromOwner);

            _damagedTargetIds.Clear();
            _currentDamage = Mathf.Max(0f, Damage * initialDamageMultiplier);

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
            if (!other.TryGetComponent<IDamagable>(out var damageable))
            {
                return;
            }

            int targetId = other.GetInstanceID();
            if (damageable is Component targetComponent)
            {
                targetId = targetComponent.GetInstanceID();
            }

            if (!_damagedTargetIds.Add(targetId))
            {
                return;
            }

            if (_currentDamage <= 0f)
            {
                ReturnToPool();
                return;
            }

            damageable.Damage(new DamageInfo(
                _currentDamage,
                Owner,
                EDamageType.Normal));

            _currentDamage *= damageMultiplierPerPierce;
            if (_currentDamage < minDamageToKeepPiercing)
            {
                ReturnToPool();
            }
        }
    }
}
