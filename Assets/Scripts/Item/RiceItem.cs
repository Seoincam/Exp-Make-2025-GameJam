using Player;
using Shared.Stat;
using UnityEngine;

public sealed class RiceItem : Item
{
    [SerializeField] private int healthAmount = 1;

    public void Initialize(int amount)
    {
        healthAmount = Mathf.Max(0, amount);
    }

    protected override void OnPickedBy(PlayerCharacter player)
    {
        if (!player || player.Stat == null)
        {
            return;
        }

        int amount = Mathf.Max(0, healthAmount);
        if (amount <= 0)
        {
            return;
        }

        player.Stat.ModifyBaseValue(StatType.Health, amount);
        player.Stat.ApplyPendingChanges();
    }
}
