using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IMonsterState
{
    void Enter();      // 최초 1회
    void Tick();       // 매 프레임
    void Exit();       // 탈출 시 1회
}
public interface IMonsterBehaviour
{
    /// ① 지금 이 패턴을 실행할 수 있는가? (조건식)
    bool CanRun(MonsterContext ctx);          // 기본값: true

    /// ② 패턴 본체 – 반드시 yield 반환
    IEnumerator Execute(MonsterContext ctx);

    /// ③ 중도 취소 시 후처리(선택)
    void OnInterrupt(MonsterContext ctx);
}
public abstract class AttackBehaviourSO : ScriptableObject, IMonsterBehaviour
{
    public virtual bool CanRun(MonsterContext ctx) => true;
    public abstract IEnumerator Execute(MonsterContext ctx);
    public virtual void OnInterrupt(MonsterContext ctx) { }
}
public sealed class MonsterStateMachine
{
    readonly Stack<IMonsterState> stack = new();

    public IMonsterState Current => stack.Count > 0 ? stack.Peek() : null;

    public void ChangeState(IMonsterState next)
    {
        while (stack.Count > 0)
        {
            var s = stack.Pop();
            s.Exit();
        }
        if (next != null)
        {
            stack.Push(next);
            next.Enter();
        }
    }
    /// 오버레이 상태(예: 스턴) 푸시
    public void PushState(IMonsterState overlay)
    {
        // 아래 상태의 비동기 태스크/코루틴 중단을 위해 Exit() 호출 후 스택에 유지
        if (Current != null) Current.Exit();      // 감지 상태의 UniTask/CTS 끊기 목적
        stack.Push(overlay);
        overlay.Enter();
    }

    /// 오버레이 상태 종료 → 아래 상태 재진입(Enter 재호출)
    public void PopState()
    {
        if (stack.Count == 0) return;
        var top = stack.Pop();
        top.Exit();
        if (Current != null) Current.Enter();     // 기존 상태 재진입(안전한 재개)
    }

    public void Tick() => Current?.Tick();
}
public enum Route
{
    None,           // 유지
    Idle,           // 대기
    Detect,         // 소리/흔적 추적(수사)
    Attack,         // 적대 전투 루프
    Special,        // 아이템/특수 상호작용
    Flee,           // 비적대 도주
    Trace           // 플레이어 추적(근접 유지)
}