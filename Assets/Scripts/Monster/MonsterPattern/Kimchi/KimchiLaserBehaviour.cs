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
        int resolvedHitMask = ResolveHitMask();
        RaycastHit2D hit = FindFirstBlockingHit(origin, fixedDir, fireRange, resolvedHitMask, ctx.mono.transform);

        float beamLen = fireRange;
        if (hit.collider != null) beamLen = hit.distance;

        bool shouldDamagePlayer = ShouldDamagePlayer(ctx, origin, fixedDir, beamLen, resolvedHitMask);

        if (beamSprite != null)
            yield return ShowBeamAndDamage(ctx, origin, fixedDir, beamLen, shouldDamagePlayer);
        else
            ApplyDamageIfPlayerHit(ctx, shouldDamagePlayer, damage, ctx.mono.gameObject);

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

    IEnumerator ShowBeamAndDamage(MonsterContext ctx, Vector2 origin, Vector2 dir, float len, bool shouldDamagePlayer)
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
        ApplyDamageIfPlayerHit(ctx, shouldDamagePlayer, damage, ctx.mono.gameObject);

        // 잠깐 표시 후 제거
        yield return new WaitForSeconds(Mathf.Max(0.01f, beamShowTime));
        Object.Destroy(go);
    }

    int ResolveHitMask()
    {
        // 0이면 아무 것도 맞지 않으므로 기본 레이어 전체로 보정
        if (hitMask.value == 0)
        {
            return Physics2D.DefaultRaycastLayers;
        }

        return hitMask.value;
    }

    static RaycastHit2D FindFirstBlockingHit(
        Vector2 origin,
        Vector2 dir,
        float distance,
        int mask,
        Transform sourceRoot)
    {
        var hits = Physics2D.RaycastAll(origin, dir, distance, mask);
        for (int i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];
            if (hit.collider == null) continue;
            if (IsSelfOrChild(hit.collider, sourceRoot)) continue;
            return hit;
        }

        return default;
    }

    bool ShouldDamagePlayer(
        MonsterContext ctx,
        Vector2 origin,
        Vector2 dir,
        float maxDistance,
        int mask)
    {
        if (ctx.player == null)
        {
            return false;
        }

        Vector2 toPlayer = (Vector2)ctx.player.position - origin;
        float along = Vector2.Dot(toPlayer, dir);

        if (along < 0f || along > maxDistance)
        {
            return false;
        }

        // dir은 정규화되어 있으므로 외적 크기 = 선분까지 수선 거리
        float perpendicularDistance = Mathf.Abs((toPlayer.x * dir.y) - (toPlayer.y * dir.x));
        float hitHalfWidth = Mathf.Max(0.05f, beamWidth * 0.5f);
        if (perpendicularDistance > hitHalfWidth)
        {
            return false;
        }

        // 플레이어까지 가는 경로에 플레이어 외 장애물이 있으면 빗맞음 처리
        var hits = Physics2D.RaycastAll(origin, dir, along, mask);
        for (int i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];
            if (hit.collider == null) continue;
            if (IsSelfOrChild(hit.collider, ctx.mono.transform)) continue;
            if (IsPlayerCollider(hit.collider, ctx.player, playerTag)) continue;
            return false;
        }

        return true;
    }

    static bool IsSelfOrChild(Collider2D collider, Transform sourceRoot)
    {
        if (collider == null || sourceRoot == null)
        {
            return false;
        }

        return collider.transform == sourceRoot || collider.transform.IsChildOf(sourceRoot);
    }

    static bool IsPlayerCollider(Collider2D collider, Transform playerRoot, string playerTag)
    {
        if (collider == null)
        {
            return false;
        }

        if (playerRoot != null &&
            (collider.transform == playerRoot || collider.transform.IsChildOf(playerRoot)))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(playerTag))
        {
            if (collider.CompareTag(playerTag))
            {
                return true;
            }

            Transform root = collider.transform.root;
            if (root != null && root.CompareTag(playerTag))
            {
                return true;
            }
        }

        return false;
    }

    static void ApplyDamageIfPlayerHit(MonsterContext ctx, bool shouldDamagePlayer, float dmg, GameObject source)
    {
        if (!shouldDamagePlayer || ctx.player == null || dmg <= 0f)
        {
            return;
        }

        IDamagable d = ctx.player.GetComponent<IDamagable>();
        if (d == null)
        {
            d = ctx.player.GetComponentInParent<IDamagable>();
        }

        if (d != null)
        {
            d.Damage(new DamageInfo(dmg, source, EDamageType.Normal));
        }
    }

    public override void OnInterrupt(MonsterContext ctx)
    {
        HideTelegraph(ctx);
        ctx.StopMovePixel();
        ctx.UnlockMove();
    }
}
