using System;
using UnityEngine;

public enum EDamageType
{
    Normal
}

[Flags]
public enum EDamageEffectFlags
{
    None = 0,
    Slow = 1 << 0,
    Stun = 1 << 1,
    Freeze = 1 << 2,
    Knockback = 1 << 3
}

public struct DamageInfo
{
    public float Damage;
    public GameObject Source;
    public EDamageType DamageType;
    public EDamageEffectFlags EffectFlags;

    public DamageInfo(
        float damage,
        GameObject source = null,
        EDamageType damageType = EDamageType.Normal,
        EDamageEffectFlags effectFlags = EDamageEffectFlags.None)
    {
        Damage = damage;
        Source = source;
        DamageType = damageType;
        EffectFlags = effectFlags;
    }
}

public interface IDamagable
{
    void Damage(DamageInfo damageInfo);
}
