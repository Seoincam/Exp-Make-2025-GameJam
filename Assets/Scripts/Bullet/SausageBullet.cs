using System.Collections.Generic;
using UnityEngine;

namespace Combat.Shoot
{
    public class SausageBullet : BulletBase
    {
        [Header("Sausage")]
        [SerializeField, Min(0.1f)] private float bounceSearchRadius = 4f;
        [SerializeField, Min(1)] private int maxBounceCount = 10;

        private readonly HashSet<int> _hitTargetIds = new();

        private Transform _currentTargetTransform;
        private int _currentTargetId = -1;
        private int _bounceCount;
        private Vector2 _moveDirection = Vector2.right;

        public override void Init(
            Transform target,
            float speed,
            float damage,
            GameObject owner,
            float maxDistanceFromOwner)
        {
            base.Init(target, speed, damage, owner, maxDistanceFromOwner);

            _hitTargetIds.Clear();
            _bounceCount = 0;
            _currentTargetTransform = null;
            _currentTargetId = -1;

            if (!TrySetTargetFromTransform(target))
            {
                ReturnToPool("NoInitialTarget");
                return;
            }

            UpdateMoveDirection();
        }

        protected override void TickMovement(float deltaTime)
        {
            if (!_currentTargetTransform)
            {
                if (!TryFindAndSetNextTarget(transform.position, -1))
                {
                    ReturnToPool("NoTarget");
                    return;
                }
            }

            UpdateMoveDirection();
            transform.Translate(_moveDirection * (Speed * deltaTime), Space.World);
        }

        protected override void OnHit(Collider2D other)
        {
            if (!TryResolveDamageable(other, out var hitDamageable, out var hitTargetTransform, out int hitTargetId))
            {
                return;
            }

            if (hitTargetId != _currentTargetId)
            {
                return;
            }

            hitDamageable.Damage(new DamageInfo(
                Damage,
                Owner,
                EDamageType.Normal));

            _hitTargetIds.Add(hitTargetId);

            if (_bounceCount >= maxBounceCount)
            {
                ReturnToPool("MaxBounceCount");
                return;
            }

            if (!TryFindAndSetNextTarget(hitTargetTransform.position, hitTargetId))
            {
                ReturnToPool("NoNextTarget");
                return;
            }

            _bounceCount++;
            UpdateMoveDirection();
        }

        private bool TryFindAndSetNextTarget(Vector2 origin, int excludeTargetId)
        {
            MonsterController bestUnhit = null;
            MonsterController bestAny = null;
            float bestUnhitDist = float.MaxValue;
            float bestAnyDist = float.MaxValue;

            var monsters = FindObjectsOfType<MonsterController>();
            foreach (var monster in monsters)
            {
                if (!monster || !monster.gameObject.activeInHierarchy || monster._killed)
                {
                    continue;
                }

                int candidateId = monster.GetInstanceID();
                if (candidateId == excludeTargetId)
                {
                    continue;
                }

                float dist = Vector2.Distance(origin, monster.transform.position);
                if (dist > bounceSearchRadius)
                {
                    continue;
                }

                if (dist < bestAnyDist)
                {
                    bestAnyDist = dist;
                    bestAny = monster;
                }

                if (!_hitTargetIds.Contains(candidateId) && dist < bestUnhitDist)
                {
                    bestUnhitDist = dist;
                    bestUnhit = monster;
                }
            }

            var picked = bestUnhit != null ? bestUnhit : bestAny;
            if (!picked)
            {
                return false;
            }

            _currentTargetTransform = picked.transform;
            _currentTargetId = picked.GetInstanceID();
            return true;
        }

        private bool TrySetTargetFromTransform(Transform target)
        {
            if (!target)
            {
                return false;
            }

            if (!TryResolveDamageable(target, out _, out var targetTransform, out int targetId))
            {
                return false;
            }

            _currentTargetTransform = targetTransform;
            _currentTargetId = targetId;
            return true;
        }

        private bool TryResolveDamageable(
            Component component,
            out IDamagable damageable,
            out Transform targetTransform,
            out int targetId)
        {
            damageable = null;
            targetTransform = null;
            targetId = -1;

            if (!component)
            {
                return false;
            }

            damageable = component.GetComponent<IDamagable>();
            if (damageable == null)
            {
                damageable = component.GetComponentInParent<IDamagable>();
            }

            if (damageable == null)
            {
                return false;
            }

            if (damageable is not Component damageableComponent)
            {
                return false;
            }

            if (damageableComponent is not MonsterController)
            {
                return false;
            }

            targetTransform = damageableComponent.transform;
            targetId = damageableComponent.GetInstanceID();
            return true;
        }

        private void UpdateMoveDirection()
        {
            if (!_currentTargetTransform)
            {
                return;
            }

            Vector2 toTarget = (Vector2)_currentTargetTransform.position - (Vector2)transform.position;
            if (toTarget.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            _moveDirection = toTarget.normalized;
            float angle = Mathf.Atan2(_moveDirection.y, _moveDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
