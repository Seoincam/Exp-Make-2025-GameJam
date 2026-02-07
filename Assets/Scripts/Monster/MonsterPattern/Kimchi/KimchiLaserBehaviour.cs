using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Behaviours/Kimchi/Laser Attack (Telegraph+Beam)")]
public class KimchiLaserBehaviourSO : AttackBehaviourSO
{
    [Header("Trigger")]
    public float fireRange = 6f;
    public float cooldown = 10f;

    [Header("Aim / Fire")]
    public float aimTime = 0.6f;
    public float beamShowTime = 0.12f;   // 발사 후 빔 보이는 시간
    public float recoverTime = 0.1f;

    [Header("Damage")]
    public float damage = 10f;

    [Header("Telegraph Rect")]
    public float telegraphWidth = 0.8f;
    [Range(0.05f, 0.6f)] public float telegraphAlpha = 0.25f;
    public Color telegraphColor = Color.red;

    [Header("Beam Visual (SpriteRenderer)")]
    public Sprite beamSprite;            // 인스펙터 할당
    public float beamWidth = 0.45f;
    [Range(0.05f, 1f)] public float beamAlpha = 0.9f;
    public Color beamColor = Color.red;
    public int beamSortingOrder = 1100;

    [Header("Raycast")]
    public LayerMask hitMask;
    public string playerTag = "Player";

    public override bool CanRun(MonsterContext ctx)
    {
        if (!ctx.player) return false;
        if (!ctx.IsReady(this)) return false;

        float dist = Vector2.Distance(ctx.transform.position, ctx.player.position);
        return dist <= fireRange;
    }

    public override IEnumerator Execute(MonsterContext ctx)
    {
        if (!ctx.player) yield break;

        // 쿨타임 선점
        ctx.SetCooldown(this, cooldown);

        // 조준/발사 동안 이동 독점
        ctx.LockMove();
        ctx.StopMovePixel();
        ctx.animationHub?.SetTag(MonsterStateTag.CombatAttack, ctx);

        // ✅ 조준 시작 시점 고정
        Vector2 origin = ctx.transform.position;
        Vector2 fixedTarget = ctx.player.position;
        Vector2 fixedDir = fixedTarget - origin;
        if (fixedDir.sqrMagnitude < 1e-6f)
        {
            ctx.UnlockMove();
            yield break;
        }
        fixedDir.Normalize();

        // ✅ aimTime 동안 텔레그래프 유지(방향 고정)
        float t = 0f;
        while (t < aimTime)
        {
            ShowTelegraph(ctx, origin, fixedDir, fireRange);
            t += Time.deltaTime;
            yield return null;
        }

        HideTelegraph(ctx);

        // ✅ 발사(방향 고정)
        RaycastHit2D hit = Physics2D.Raycast(origin, fixedDir, fireRange, hitMask);

        float beamLen = fireRange;
        if (hit.collider != null) beamLen = hit.distance;

        if (beamSprite != null)
            yield return ShowBeamAndDamage(ctx, origin, fixedDir, beamLen, hit);
        else
            ApplyDamageIfPlayerHit(hit, playerTag, damage, ctx.mono.gameObject);

        if (recoverTime > 0f)
            yield return new WaitForSeconds(recoverTime);

        ctx.UnlockMove();
    }

    void ShowTelegraph(MonsterContext ctx, Vector2 origin, Vector2 dir, float len)
    {
        ctx.mono.EnsureTelegraph();
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Vector2 center = origin + dir * (len * 0.5f);

        var c = telegraphColor; c.a = telegraphAlpha;
        ctx.mono.Telegraph.Show(center, len, telegraphWidth, angle, c);
    }

    void HideTelegraph(MonsterContext ctx)
    {
        ctx.mono?.Telegraph?.Hide();
    }

    IEnumerator ShowBeamAndDamage(MonsterContext ctx, Vector2 origin, Vector2 dir, float len, RaycastHit2D hit)
    {
        // 빔 오브젝트 생성(매번 만들기 싫으면 MonsterController에 캐시해도 됨)
        var go = new GameObject($"{ctx.mono.name}_LaserBeam");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = beamSprite;
        sr.sortingOrder = beamSortingOrder;

        var c = beamColor; c.a = Mathf.Clamp01(beamAlpha);
        sr.color = c;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Vector2 center = origin + dir * (len * 0.5f);

        go.transform.position = new Vector3(center.x, center.y, 0f);
        go.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        go.transform.localScale = new Vector3(len, beamWidth, 1f);

        // 데미지 1회
        ApplyDamageIfPlayerHit(hit, playerTag, damage, ctx.mono.gameObject);

        // 잠깐 표시 후 제거
        yield return new WaitForSeconds(Mathf.Max(0.01f, beamShowTime));
        Object.Destroy(go);
    }

    static void ApplyDamageIfPlayerHit(RaycastHit2D hit, string playerTag, float dmg, GameObject source)
    {
        if (hit.collider == null) return;
        if (!hit.collider.CompareTag(playerTag)) return;

        if (hit.collider.TryGetComponent<IDamagable>(out var d))
            d.Damage(new DamageInfo(dmg, source, EDamageType.Normal));
    }

    public override void OnInterrupt(MonsterContext ctx)
    {
        HideTelegraph(ctx);
        ctx.StopMovePixel();
        ctx.UnlockMove();
    }
}
