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
        ctx.SightLocked = false;
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

        ctx.MoveTowardPlayerPixel(Time.deltaTime);
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
            Switch(Route.Idle);
            return;
        }

        float dist = Vector2.Distance(ctx.transform.position, ctx.player.position);

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