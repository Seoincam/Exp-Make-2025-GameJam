using Shared.Stat;
using UnityEngine;

namespace Combat.Shoot
{
    /// <summary>
    /// Stat과 EffectManager를 사용하는 엔티티가 구현함.
    /// </summary>
    public interface IEntity
    {
        Stat Stat { get; }
        EffectManager EffectManager { get; }
        Transform Transform { get; }
    }
}