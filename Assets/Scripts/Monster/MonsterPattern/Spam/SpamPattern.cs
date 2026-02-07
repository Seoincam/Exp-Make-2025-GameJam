using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Behaviours/Spam/Y Offset Charge Attack (NoStop NoDir)")]
public class SpamYChargeAttackBehaviourSO : AttackBehaviourSO
{
    [Header("Trigger")]
    public float yRange = 2.0f;
    public float yOffsetN = 2.0f;
    public float cooldown = 10f;

    [Header("Telegraph")]
    public float windupTime = 0.5f;

    [Header("Dash")]
    public float dashSpeed = 10f;
    public float maxDashDistance = 0f;

    [Header("Damage (once)")]
    public string playerTag = "Player";
    public float hitRadius = 0.5f;      // 근접 판정 반경(원)
    public int damage = 20;

    [Header("Pixel Snap (Optional)")]
    public int pixelsPerUnit = 0;

    public override bool CanRun(MonsterContext ctx)
    {
        if (!ctx.player) return false;
        if (!ctx.IsReady(this)) return false;

        float dy = Mathf.Abs(ctx.player.position.y - ctx.transform.position.y);
        return dy >= yRange;
    }

    public override IEnumerator Execute(MonsterContext ctx)
    {
        if (!ctx.player) yield break;

        // 쿨타임 선점
        ctx.SetCooldown(this, cooldown);

        // 이동 독점(직선 보장 핵심)
        ctx.LockMove();

        // 목표 고정: 시작 순간의 (player.x, player.y + n)
        Vector2 startPos = ctx.transform.position;
        Vector2 fixedTarget = new Vector2(ctx.player.position.x, ctx.player.position.y + yOffsetN);

        Vector2 deltaToTarget = fixedTarget - startPos;
        if (deltaToTarget.sqrMagnitude < 1e-6f)
        {
            ctx.UnlockMove();
            yield break;
        }

        Vector2 dir = deltaToTarget.normalized;

        if (windupTime > 0f)
            yield return new WaitForSeconds(windupTime);

        float travelled = 0f;
        float limit = (maxDashDistance > 0f) ? maxDashDistance : deltaToTarget.magnitude;

        bool hitApplied = false;

        while (travelled < limit)
        {
            float step = dashSpeed * Time.deltaTime;
            if (travelled + step > limit) step = limit - travelled;

            Vector2 next = (Vector2)ctx.transform.position + dir * step;

            // 근접 1회 데미지(한 번만)
            if (!hitApplied && ctx.player)
            {
                float dist = Vector2.Distance(next, ctx.player.position);
                if (dist <= hitRadius)
                {
                    ApplyDamageToPlayer(damage);
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
        // 중단 시에도 잠금 해제
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

    static void ApplyDamageToPlayer(int rawDamage)
    {
        Debug.Log($"플레이어에게 {rawDamage}데미지");
        // TODO: 플레이어 HP 시스템 연결
    }
}
