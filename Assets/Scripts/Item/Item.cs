using Player;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public abstract class Item : MonoBehaviour
{
    [Header("Pickup")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool destroyOnPickup = true;
    private bool _picked;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col && !col.isTrigger)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_picked || !other)
        {
            return;
        }

        var player = other.GetComponentInParent<PlayerCharacter>();
        if (!player)
        {
            return;
        }

        if (!string.IsNullOrEmpty(playerTag) && !player.CompareTag(playerTag))
        {
            return;
        }

        _picked = true;

        OnPickedBy(player);

        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }
    }

    protected abstract void OnPickedBy(PlayerCharacter player);
}
