using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Behaviours/Spam/Y Offset Charge Attack (OnceHit Straight)")]
public class SpamYChargeAttackBehaviourSO : AttackBehaviourSO
{
    [Header("Trigger")]
    [Tooltip("|playerY - monsterY| >= yRange 이면 발동")]
    public float yRange = 2.0f;

    [Tooltip("목표 지점: player.y + n")]
    public float yOffsetN = 2.0f;

    [Tooltip("내부 쿨타임(초)")]
    public float cooldown = 10f;

    [Header("Telegraph")]
    [Tooltip("돌진 전 대기(초)")]
    public float windupTime = 0.5f;

    [Header("Dash")]
    [Tooltip("돌진 속도(월드/초)")]
    public float dashSpeed = 10f;

    [Tooltip("최대 돌진 거리(0이면 목표 지점까지)")]
    public float maxDashDistance = 0f;

    [Header("Damage (Once)")]
    [Tooltip("이 반경 안으로 들어오면 딱 1회 데미지")]
    public float hitRadius = 0.5f;

    public int damage = 20;

    [Header("Pixel Snap (Optional)")]
    [Tooltip("0이면 스냅 안 함. 사용하면 계단형으로 보일 수 있음")]
    public int pixelsPerUnit = 0;

    public override bool CanRun(MonsterContext ctx)
    {
        if (ctx.player == null) return false;
        if (!ctx.IsReady(this)) return false;

        float dy = Mathf.Abs(ctx.player.position.y - ctx.transform.position.y);
        return dy >= yRange;
    }

    public override IEnumerator Execute(MonsterContext ctx)
    {
        if (ctx.player == null) yield break;

        // 쿨타임 선점
        ctx.SetCooldown(this, cooldown);

        // 이동 독점(직선 돌진 보장)
        ctx.LockMove();

        // 픽셀 이동 누적값이 있다면, 돌진 전에 동기화(드리프트 방지)
        ctx.StopMovePixel();

        // 목표 고정: 시작 순간의 (player.x, player.y + n)
        Vector2 startPos = ctx.transform.position;
        Vector2 targetPos = new Vector2(ctx.player.position.x, ctx.player.position.y + yOffsetN);

        Vector2 toTarget = targetPos - startPos;
        if (toTarget.sqrMagnitude < 1e-6f)
        {
            ctx.UnlockMove();
            yield break;
        }

        Vector2 dir = toTarget.normalized;

        if (windupTime > 0f)
            yield return new WaitForSeconds(windupTime);

        float limit = (maxDashDistance > 0f) ? maxDashDistance : toTarget.magnitude;
        float travelled = 0f;

        bool hitApplied = false;

        while (travelled < limit)
        {
            float step = dashSpeed * Time.deltaTime;
            if (travelled + step > limit) step = limit - travelled;

            Vector2 next = (Vector2)ctx.transform.position + dir * step;

            // 근접 1회 데미지
            if (!hitApplied && ctx.player != null)
            {
                float d = Vector2.Distance(next, ctx.player.position);
                if (d <= hitRadius)
                {
                    ApplyDamageToPlayer(ctx, damage);
                    hitApplied = true;
                }
            }

            ctx.transform.position = SnapIfNeeded(next);
            travelled += step;

            yield return null;
        }

        ctx.UnlockMove();
    }

    public override void OnInterrupt(MonsterContext ctx)
    {
        ctx.UnlockMove();
    }

    Vector2 SnapIfNeeded(Vector2 pos)
    {
        if (pixelsPerUnit <= 0) return pos;
        float ppu = Mathf.Max(1, pixelsPerUnit);
        float x = Mathf.Round(pos.x * ppu) / ppu;
        float y = Mathf.Round(pos.y * ppu) / ppu;
        return new Vector2(x, y);
    }

    static void ApplyDamageToPlayer(MonsterContext ctx, int rawDamage)
    {
        if (ctx.player == null) return;

        // MonsterController가 쓰는 방식과 동일: Player가 IDamagable이면 DamageInfo로 전달
        if (ctx.player.TryGetComponent<IDamagable>(out var damageable))
        {
            damageable.Damage(new DamageInfo(rawDamage, ctx.mono.gameObject, EDamageType.Normal));
        }
    }
}
