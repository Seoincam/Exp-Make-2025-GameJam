using UnityEngine;

public abstract class AnimationPlayerBase : MonoBehaviour
{
    /// 이 플레이어가 현재 컨텍스트의 몬스터에 적용 가능한지.
    /// 대부분의 경우 true 그대로 두면 됩니다. (특정 몬스터에만 쓰고 싶으면 오버라이드)
    public virtual bool IsActiveFor(MonsterContext ctx) => true;

    /// 상태 Enter 때마다 허브가 호출합니다.
    public abstract void SetTag(MonsterStateTag tag, MonsterContext ctx);
}
public enum MonsterStateTag
{
    None,
    Idle, Detect, Flee, Special, Trace, Return, Stun, SearchWander, Killed,
    CombatMove, CombatAttack, BackStep, SpinAttack, PreSpinAttack, Hide, Change, ReChange, Roll
}