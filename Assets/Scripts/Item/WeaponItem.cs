using Player;
using Player.State;
using Shared.Stat;
using UnityEngine;

public sealed class WeaponItem : Item
{
    [SerializeField] private PlayerState weaponType = PlayerState.Anchovy;
    [SerializeField] private float baseHealthAmount = 10f;
    [SerializeField] private float weaponHealthAmount = 10f;

    protected override void OnPickedBy(PlayerCharacter player)
    {
        if (!player)
        {
            return;
        }

        player.ChangeState(weaponType, true);

        if (player.Stat == null)
        {
            return;
        }

        bool hasPendingStatChange = false;

        float healthAmount = Mathf.Max(0f, baseHealthAmount);
        if (healthAmount > 0f)
        {
            player.Stat.ModifyBaseValue(StatType.Health, healthAmount);
            hasPendingStatChange = true;
        }

        float weaponAmount = Mathf.Max(0f, weaponHealthAmount);
        if (weaponAmount > 0f && TryGetWeaponStatType(weaponType, out var statType))
        {
            player.Stat.ModifyBaseValue(statType, weaponAmount);
            hasPendingStatChange = true;
        }

        if (hasPendingStatChange)
        {
            player.Stat.ApplyPendingChanges();
        }
    }

    private static bool TryGetWeaponStatType(PlayerState state, out StatType statType)
    {
        switch (state)
        {
            case PlayerState.Anchovy:
                statType = StatType.AnchovyBullet;
                return true;
            case PlayerState.FlyingFishRoe:
                statType = StatType.FlyingFishRoeBullet;
                return true;
            case PlayerState.Sausage:
                statType = StatType.SausageBullet;
                return true;
            case PlayerState.Garlic:
                statType = StatType.GarlicBullet;
                return true;
            case PlayerState.ChiliPepperAndTuna:
                statType = StatType.ChiliPepperAndTunaBullet;
                return true;
            default:
                statType = default;
                return false;
        }
    }
}
