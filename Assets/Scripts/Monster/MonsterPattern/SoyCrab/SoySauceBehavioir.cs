using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Behaviours/Ganjang/Spawn Soy Field")]
public class GanjangSoyFieldBehaviourSO : AttackBehaviourSO
{
    [Header("Prefab")]
    public GameObject fieldPrefab;

    [Header("Trigger")]
    [Tooltip("플레이어가 이 거리 이내면 발동")]
    public float triggerRange = 6f;
    [Tooltip("추가 보정(+n)")]
    public float rangeOffsetN = 0f;

    [Header("Cooldown")]
    public float cooldown = 20f;

    [Header("Field Spec")]
    public float duration = 10f;
    [Range(0f, 3f)] public float speedBonus01 = 0.30f;
    public float width = 3f;
    public float height = 2f;

    [Header("Spawn Position")]
    [Tooltip("플레이어 위치에 깔기(권장)")]
    public bool spawnAtPlayer = true;

    [Tooltip("spawnAtPlayer=false일 때, 몬스터->플레이어 방향으로 이 거리만큼 앞에 깔기")]
    public float spawnForwardDistance = 2f;

    public override bool CanRun(MonsterContext ctx)
    {
        if (ctx.player == null) return false;
        if (!ctx.IsReady(this)) return false;
        if (fieldPrefab == null) return false;

        float dist = Vector2.Distance(ctx.transform.position, ctx.player.position);
        return dist <= (triggerRange + rangeOffsetN);
    }

    public override IEnumerator Execute(MonsterContext ctx)
    {
        if (ctx.player == null || fieldPrefab == null) yield break;

        // 쿨 선점
        ctx.SetCooldown(this, Mathf.Max(0.01f, cooldown));

        // 설치 동안 끼어들지 않게(원래 의도 유지)
        ctx.LockMove();
        ctx.StopMovePixel();
        ctx.animationHub?.SetTag(MonsterStateTag.CombatAttack, ctx);

        Vector3 spawnPos = ComputeSpawnPos(ctx);
        var go = Object.Instantiate(fieldPrefab, spawnPos, Quaternion.identity);

        EnsureFieldPrefabRuntimeSafety(go);

        var field = go.GetComponent<SoySauceField>();
        if (field != null)
        {
            // Trigger 기반 SoySauceField Init 시그니처에 맞춤
            field.Init(duration, speedBonus01, new Vector2(width, height));
        }
        else
        {
            Debug.LogWarning($"[GanjangSoyFieldBehaviourSO] fieldPrefab에 SoySauceField가 없습니다: {fieldPrefab.name}");
        }

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
            Vector2 origin = ctx.transform.position;
            Vector2 toPlayer = (Vector2)ctx.player.position - origin;
            Vector2 dir = (toPlayer.sqrMagnitude < 1e-6f) ? Vector2.right : toPlayer.normalized;
            spawnPos = origin + dir * spawnForwardDistance;
        }

        spawnPos.z = 0f;
        return spawnPos;
    }

    static void EnsureFieldPrefabRuntimeSafety(GameObject go)
    {
        // 1) Trigger Collider 보장
        var box = go.GetComponent<BoxCollider2D>();
        if (box == null) box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;

        // 2) Trigger 이벤트가 안 뜨는 케이스 방지:
        //    (몬스터/플레이어 쪽 Rigidbody2D가 없는 프로젝트도 있어서)
        //    장판에 Kinematic Rigidbody2D를 하나 붙여둠.
        var rb = go.GetComponent<Rigidbody2D>();
        if (rb == null) rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        // 참고: Transform Scale이 (1,1,1)이 아닌 프리팹이면
        // Collider size가 의도와 다르게 보일 수 있으니 프리팹에서 스케일은 1 권장.
    }

    public override void OnInterrupt(MonsterContext ctx)
    {
        ctx.StopMovePixel();
        ctx.UnlockMove();
    }
}
