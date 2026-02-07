using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Behaviours/Ganjang/Approach Player (Pixel Interval)")]
public class GanjangApproachBehaviourSO : AttackBehaviourSO
{
    [Header("Approach")]
    [Tooltip("이 거리 이내로 접근하면 행동 종료")]
    public float desiredDistance = 2.2f;

    [Tooltip("한 번 실행될 때 이동을 수행하는 시간(초). 끝나면 AttackState가 다음 행동을 다시 선택함")]
    public float actionInterval = 1.0f;

    public override bool CanRun(MonsterContext ctx)
    {
        if (!ctx.player) return false;
        return Vector2.Distance(ctx.transform.position, ctx.player.position) > desiredDistance;
    }

    public override IEnumerator Execute(MonsterContext ctx)
    {
        if (!ctx.player) yield break;

        ctx.LockMove();
        ctx.animationHub?.SetTag(MonsterStateTag.CombatMove, ctx);

        float t = 0f;

        while (ctx.player &&
               Vector2.Distance(ctx.transform.position, ctx.player.position) > desiredDistance)
        {
            ctx.MoveTowardPlayer_KeepDistance(Time.deltaTime);

            t += Time.deltaTime;
            if (t >= actionInterval)
                break;

            yield return null;
        }

        ctx.StopMovePixel();
        ctx.UnlockMove();
    }

    public override void OnInterrupt(MonsterContext ctx)
    {
        ctx.StopMovePixel();
        ctx.UnlockMove();
    }
}
