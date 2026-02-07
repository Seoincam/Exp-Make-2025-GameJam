using UnityEngine;

namespace Combat.Shoot
{
    public class Bullet : MonoBehaviour
    {
        private Vector2 _direction;
        private float _speed;
        private float _lifetime;
        private float _damage;
        private GameObject _owner;

        public void Init(Vector2 direction, float speed, float lifetime, float damage, GameObject owner)
        {
            _direction = direction;
            _speed = speed;
            _lifetime = lifetime;
            _damage = damage;
            _owner = owner;

            Debug.Log($"Bullet: initialized (damage={_damage}, owner={_owner?.name})");
        }

        private void Update()
        {
            transform.Translate(_direction * (_speed * Time.deltaTime), Space.World);

            _lifetime -= Time.deltaTime;
            if (_lifetime <= 0f)
            {
                Debug.Log("Bullet: lifetime ended.");
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent<IDamagable>(out var damageable))
            {
                Debug.Log($"Bullet: hit {other.name}");
                damageable.Damage(new DamageInfo(
                    _damage,
                    _owner,
                    EDamageType.Normal));
                Destroy(gameObject);
            }
        }
    }
}
