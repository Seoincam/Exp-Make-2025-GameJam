using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CurrencyPickup : MonoBehaviour
{
    public static event Action<int> OnCollected;

    [SerializeField] int amount = 1;

    [Header("Pickup")]
    [SerializeField] string playerTag = "Player";
    [SerializeField] float autoDestroySeconds = 20f;

    [Header("Spawn Bounce (Optional)")]
    [SerializeField] float bounceRadius = 0.15f;
    [SerializeField] float bounceSeconds = 0.12f;

    float _life;
    Vector3 _spawnPos;
    bool _initialized;

    public void Init(int amount)
    {
        this.amount = Mathf.Max(0, amount);
        _initialized = true;
    }

    void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        _life = autoDestroySeconds;
        _spawnPos = transform.position;

        // 간단한 스폰 튕김(원치 않으면 bounceSeconds=0)
        if (bounceSeconds > 0f)
        {
            Vector2 rnd = UnityEngine.Random.insideUnitCircle.normalized;
            transform.position = _spawnPos + (Vector3)(rnd * bounceRadius);
        }
    }

    void Update()
    {
        if (autoDestroySeconds > 0f)
        {
            _life -= Time.deltaTime;
            if (_life <= 0f) Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other || !other.CompareTag(playerTag)) return;

        int a = Mathf.Max(0, amount);
        if (a > 0) OnCollected?.Invoke(a);

        Destroy(gameObject);
    }
}
