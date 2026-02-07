using UnityEngine;

public enum EDamageType
{
    Normal
}

public struct DamageInfo
{
    public float Damage;
    public GameObject Source;
    public EDamageType DamageType;

    public DamageInfo(float damage, GameObject source = null, EDamageType damageType = EDamageType.Normal)
    {
        Damage = damage;
        Source = source;
        DamageType = damageType;
    }
}

public interface IDamagable
{
    void Damage(DamageInfo damageInfo);
}
