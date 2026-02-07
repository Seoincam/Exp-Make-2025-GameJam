using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Behaviours/Ganjang/Spawn Soy Field")]
public class GanjangSoyFieldBehaviourSO : AttackBehaviourSO
{
    [Header("Trigger")]
    public float triggerRange = 6f;
    public float rangeOffsetN = 0f;

    [Header("Cooldown")]
    public float cooldown = 20f;

    [Header("Field Spec")]
    public float duration = 10f;
    [Range(0f, 3f)] public float speedBonus01 = 0.30f;
    public float width = 3f;
    public float height = 2f;

    [Header("Layer Filter")]
    public LayerMask monsterMask;
    public LayerMask playerMask;

    [Header("Spawn Position")]
    public bool spawnAtPlayer = true;
    public float spawnForwardDistance = 2f;

    [Header("Player Damage Hook")]
    public float playerTickInterval = 0.5f;
    public float playerTickDamage = 5f;

    [Header("Debug")]
    public bool debugLog = false;

    public override bool CanRun(MonsterContext ctx)
    {
        if (ctx.player == null) return false;
        if (!ctx.IsReady(this)) return false;

        float dist = Vector2.Distance(ctx.transform.position, ctx.player.position);
        return dist <= (triggerRange + rangeOffsetN);
    }

    public override IEnumerator Execute(MonsterContext ctx)
    {
        if (ctx.player == null) yield break;

        ctx.SetCooldown(this, Mathf.Max(0.01f, cooldown));

        ctx.LockMove();
        ctx.StopMovePixel();
        ctx.animationHub?.SetTag(MonsterStateTag.CombatAttack, ctx);

        Vector3 spawnPos = ComputeSpawnPos(ctx);
        SoySauceField.Spawn(
            position: spawnPos,
            duration: duration,
            speedBonus01: speedBonus01,
            size: new Vector2(width, height),
            monsterMask: monsterMask,
            playerMask: playerMask,
            playerTickInterval: playerTickInterval,
            playerTickDamage: playerTickDamage,
            debugLog: debugLog
        );

        yield return null;
        ctx.UnlockMove();
    }

    Vector3 ComputeSpawnPos(MonsterContext ctx)
    {
        Vector3 spawnPos;

        if (spawnAtPlayer)
        {
            spawnPos = ctx.player.position;
        }
        else
        {
            Vector2 origin = (Vector2)ctx.transform.position;
            Vector2 toPlayer = (Vector2)ctx.player.position - origin;
            Vector2 dir = (toPlayer.sqrMagnitude < 1e-6f) ? Vector2.right : toPlayer.normalized;

            Vector2 spawn2 = origin + dir * spawnForwardDistance;

            spawnPos = new Vector3(spawn2.x, spawn2.y, 0f);
        }

        spawnPos.z = 0f;
        return spawnPos;
    }

    public override void OnInterrupt(MonsterContext ctx)
    {
        ctx.StopMovePixel();
        ctx.UnlockMove();
    }
}
