using Player;
using Player.State;
using Shared.Stat;
using UnityEngine;

public sealed class WeaponItem : Item
{
    private const string BulletSoResourceRoot = "Prefabs/BulletSO";

    [SerializeField] private PlayerState weaponType = PlayerState.Anchovy;
    [SerializeField] private float baseHealthAmount = 10f;
    
    [Header("Runtime (From SO)")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int bulletCount = 10;

    private bool _loadedFromSo;
    private PlayerState _loadedType;

    public void Initialize(PlayerState type)
    {
        weaponType = type;
        LoadFromBulletSO(forceReload: true);
    }

    protected override void OnPickedBy(PlayerCharacter player)
    {
        if (!player)
        {
            return;
        }

        LoadFromBulletSO(forceReload: false);

        player.ChangeState(weaponType, true);
        player.SetBulletPrefab(bulletPrefab);

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

        float weaponAmount = Mathf.Max(0f, bulletCount);
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

    private void LoadFromBulletSO(bool forceReload)
    {
        if (!forceReload && _loadedFromSo && _loadedType == weaponType)
        {
            return;
        }

        string path = $"{BulletSoResourceRoot}/{weaponType}";
        var so = Resources.Load<global::BulletSO>(path);
        if (!so)
        {
            Debug.LogWarning($"[{nameof(WeaponItem)}] BulletSO not found at Resources/{path}. name should match enum ({weaponType}).");
            return;
        }

        bulletPrefab = so.BulletPrefab;
        bulletCount = so.BulletCount;
        _loadedFromSo = true;
        _loadedType = weaponType;
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
