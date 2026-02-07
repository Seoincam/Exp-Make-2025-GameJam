using Player;
using Shared.Stat;
using UnityEngine;

/// <summary>
/// 체력과 경험치를 얻음.
/// </summary>
public sealed class RiceItem : Item
{
    [SerializeField] private int healthAmount = 1;

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

        var spec = Effect.CreateSpec(EffectType.RiceItem)
            .AddHandler(new InstantStatHandler(StatType.Health, amount))
            .AddHandler(new InstantStatHandler(StatType.Exp, amount));
        player.EffectManager.AddEffect(spec);
    }
}
