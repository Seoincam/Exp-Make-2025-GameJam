using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Behaviours/Kimchi/Approach Player (Pixel Interval)")]
public class KimchiApproachBehaviourSO : AttackBehaviourSO
{
    [Tooltip("이 거리 이내로 접근할 때까지 실행")]
    public float desiredDistance = 2f;

    [Tooltip("행동 틱 간격 (초)")]
    public float actionInterval = 1f;   // ← 1초마다 행동

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

        float timer = 0f;

        while (ctx.player &&
               Vector2.Distance(ctx.transform.position, ctx.player.position) > desiredDistance)
        {
            // 이동
            ctx.MoveTowardPlayer_KeepDistance(Time.deltaTime);

            // 1초 누적
            timer += Time.deltaTime;
            if (timer >= actionInterval)
                break;   // ← 1초마다 행동 종료 → AttackState가 다시 선택

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
