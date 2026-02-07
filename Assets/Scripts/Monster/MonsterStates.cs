using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR;
using UnityEngine;

public sealed class MonsterIdleState : IMonsterState
{
    readonly MonsterContext ctx;
    readonly MonsterStateMachine machine;

    public MonsterIdleState(MonsterContext c, MonsterStateMachine m)
    { ctx = c; machine = m; }

    public void Enter()
    {
        ctx.animationHub?.SetTag(MonsterStateTag.Idle, ctx);
        ctx.StopMovePixel();
    }

    public void Tick()
    {
        if (ctx.player)
        {
            machine.ChangeState(new MonsterDetectState(ctx, machine));
            return;
        }
    }

    public void Exit() { }
}

// DetectState
public sealed class MonsterDetectState : IMonsterState
{
    readonly MonsterContext ctx;
    readonly MonsterStateMachine machine;

    public MonsterDetectState(MonsterContext c, MonsterStateMachine m)
    { ctx = c; machine = m; }

    public void Enter()
    {
        ctx.animationHub?.SetTag(MonsterStateTag.Detect, ctx);
    }

    public void Tick()
    {
        if (!ctx.player)
        {
            machine.ChangeState(new MonsterIdleState(ctx, machine));
            return;
        }

        float dist = Vector2.Distance(ctx.transform.position, ctx.player.position);

        if (dist <= ctx.data.attackEnterDistance)
        {
            ctx.StopMovePixel();
            machine.ChangeState(new AttackState(ctx, machine));
            return;
        }

        if (!ctx.MoveLocked)
            ctx.MoveTowardPlayer_KeepDistance(Time.deltaTime);
    }

    public void Exit()
    {
        ctx.StopMovePixel();
    }
}
public sealed class MonsterKilledState : IMonsterState
{
    readonly MonsterContext ctx;
    readonly MonsterController ctr;
    readonly MonsterStateMachine root;
    readonly GameObject go;

    public MonsterKilledState(MonsterContext c, MonsterStateMachine m, GameObject go, MonsterController mc)
    { ctx = c; root = m; this.go = go; ctr = mc; }

    public void Enter()
    {
        ctx.animationHub?.SetTag(MonsterStateTag.Killed, ctx);
        ctr.BeginDeathSequence();
    }

    public void Tick() { }
    public void Exit() { }
}
public sealed class AttackState : IMonsterState
{
    readonly MonsterContext ctx;
    readonly MonsterStateMachine root;

    Coroutine running;
    IMonsterBehaviour currentBeh;

    public AttackState(MonsterContext c, MonsterStateMachine r) { ctx = c; root = r; }

    public void Enter()
    {
        ctx.animationHub?.SetTag(MonsterStateTag.CombatAttack, ctx);
        ctx.StopMovePixel();
        SelectAndRun();
    }

    public void Tick()
    {
        if (!ctx.player)
        {
            // 실행 중이면 그냥 인터럽트 후 Idle
            Switch(Route.Idle);
            return;
        }

        // 패턴(돌진 등)이 이동을 독점 중이면
        // - 거리 기반 상태 전환(attackExitDistance) 금지
        // - StopMovePixel로 위치 동기화하지 말기
        if (ctx.MoveLocked)
            return;

        // 실행 중 코루틴이 있으면(공격 패턴 진행 중) 재선택/전환 최소화
        // (원하면 아래 거리 체크는 유지 가능하지만, 보통은 패턴 끝날 때까지 기다림)
        if (running != null)
            return;

        float dist = Vector2.Distance(ctx.transform.position, ctx.player.position);

        // 공격 상태 유지 조건을 “근접 유지”로 쓰고 싶다면 여기서만
        if (dist >= ctx.data.attackExitDistance)
        {
            Switch(Route.Detect);
            return;
        }

        ctx.StopMovePixel();
    }

    public void Exit()
    {
        Interrupt();
        ctx.StopMovePixel();
    }

    void SelectAndRun()
    {
        Interrupt();

        IMonsterBehaviour beh = PickFirst(ctx.data.combatAttackBehaviours);
        if (beh == null) beh = PickWeighted(ctx.data.combatMoveBehaviours, ctx.data.moveWeights);

        if (beh != null)
        {
            currentBeh = beh;
            running = ctx.mono.StartCoroutine(RunBehaviour(beh));
        }
        else
        {
            Switch(Route.Detect);
        }
    }

    IEnumerator RunBehaviour(IMonsterBehaviour beh)
    {
        yield return beh.Execute(ctx);
        running = null;
        currentBeh = null;

        if (root.Current == this) SelectAndRun();
    }

    void Interrupt()
    {
        if (running != null)
        {
            ctx.mono.StopCoroutine(running);
            currentBeh?.OnInterrupt(ctx);
            running = null;
            currentBeh = null;
        }
    }

    void Switch(Route r)
    {
        Interrupt();
        ctx.isCombat = false;

        switch (r)
        {
            case Route.Detect: root.ChangeState(new MonsterDetectState(ctx, root)); break;
            case Route.Idle: root.ChangeState(new MonsterIdleState(ctx, root)); break;
            default: root.ChangeState(new MonsterIdleState(ctx, root)); break;
        }
    }

    IMonsterBehaviour PickFirst(AttackBehaviourSO[] list)
    {
        if (list == null) return null;
        foreach (var b in list)
            if (b && b.CanRun(ctx)) return b;
        return null;
    }

    IMonsterBehaviour PickWeighted(AttackBehaviourSO[] list, float[] weights)
    {
        if (list == null || list.Length == 0 || weights == null || weights.Length < list.Length)
            return PickFirst(list);

        float total = 0f;
        var tmp = new float[list.Length];

        for (int i = 0; i < list.Length; i++)
        {
            bool ok = list[i] && list[i].CanRun(ctx);
            tmp[i] = ok ? Mathf.Max(0f, weights[i]) : 0f;
            total += tmp[i];
        }
        if (total <= 0f) return null;

        float pick = UnityEngine.Random.value * total;
        float acc = 0f;

        for (int i = 0; i < list.Length; i++)
        {
            acc += tmp[i];
            if (pick <= acc) return list[i];
        }
        return null;
    }
}

public class AutoDestroy : MonoBehaviour
{
    float life;
    public void Init(float sec) => life = sec;
    void Update()
    {
        life -= Time.deltaTime;
        if (life <= 0f) Destroy(gameObject);
    }
}