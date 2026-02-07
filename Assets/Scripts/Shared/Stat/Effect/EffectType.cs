namespace Shared.Stat
{
    public enum EffectType
    {
        Test,
        Slow,
        Stun,
        Freeze,
        Burn,
        
        /// <summary>
        /// 무기 '마늘' 착용 시, 플레이어 주변에 광역 데미지 제공.
        /// </summary>
        GarlicAreaDamage,
    }
}
