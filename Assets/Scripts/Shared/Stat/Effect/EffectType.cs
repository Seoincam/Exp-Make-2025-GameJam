namespace Shared.Stat
{
    public enum EffectType
    {
        Test,
        Slow,
        Stun,
        Freeze,
        Burn,
        
        RiceItem,
        
        /// <summary>
        /// 마늘로 인한 이동 속도 버프.
        /// </summary>
        GarlicSpeedBuff,
        
        /// <summary>
        /// 상대로부터 데미지를 받는 효과를 나타냄.
        /// </summary>
        Damage,
        
        /// <summary>
        /// 무기 '마늘' 착용 시, 플레이어 주변에 광역 데미지 제공하는 효과를 나타냄.
        /// </summary>
        GarlicAreaDamage,
    }
}
