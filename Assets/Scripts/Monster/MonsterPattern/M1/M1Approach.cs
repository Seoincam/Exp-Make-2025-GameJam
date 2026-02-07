using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Behaviours/M1/Approach Player")]
public class M1ApproachBehaviourSO : AttackBehaviourSO
{
    [Tooltip("이 거리 이내로 접근할 때까지 실행")]
    public float desiredDistance = 2f;
    string _runLoopClipKey;
    public override bool CanRun(MonsterContext ctx)
        => Vector2.Distance(ctx.transform.position, ctx.player.position) > desiredDistance;

    public override IEnumerator Execute(MonsterContext ctx)
    {
        /*
        SoundManager.Instance.PlaySound3D(
            "SFX_HoundRun",
            ctx.transform,
            delay: 0f,
            isLoop: true,
            type: SoundType.SFX,
            attachToTarget: true,
            minDistance: 0f,
            maxDistance: 30f);
        */
        ctx.animationHub?.SetTag(MonsterStateTag.CombatMove, ctx);
        // 원하는 거리까지 계속 이동 
        while (Vector2.Distance(ctx.transform.position, ctx.player.position) > desiredDistance)
        {
            //ctx.agent.SetDestination(ctx.player.position);
            yield return null;
        }
        int roll = Random.Range(0, 100);
        if (roll < 30) ctx.nextBehaviourIndex = 0;   // 근접공격
        else ctx.nextBehaviourIndex = 2;             // Orbit
    }
}
