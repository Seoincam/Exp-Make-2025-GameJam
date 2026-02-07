using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;
public sealed class MonsterIdleState : IMonsterState
{
    readonly MonsterContext ctx;
    readonly MonsterStateMachine machine;

    float restTimer;
    float detectGate;   // Detect 전 대기 누적
    float returnGate;   // Return 전 대기 누적
    bool bettleMode;
    public MonsterIdleState(MonsterContext c, MonsterStateMachine m)
    { ctx = c; machine = m; }

    public void Enter()
    {
        ctx.animationHub?.SetTag(MonsterStateTag.Idle, ctx);

    }

    public void Tick()
    {
        // TODO : 현재는 그냥 순서대로 가중치를 둬서 로직을 변경하지만 가중치를 설정해 분기 필요
        Route r = ctx.hub.Decide(Time.deltaTime);
        if (TryRoute(r)) return;

        restTimer -= Time.deltaTime;
        if (restTimer > 0f) return;



    }
    bool TryRoute(Route r)
    {
        /*
        switch (r)
        {
            case Route.Detect:
                machine.ChangeState(new MonsterDetectState(ctx, machine));
                { ctx.agent.isStopped = false; return true; }
            case Route.Special:
                machine.ChangeState(new MonsterSpecialState(ctx, machine));
                { ctx.agent.isStopped = false; return true; }
            case Route.Attack:
                machine.ChangeState(new CombatSuperState(ctx, machine));
                { ctx.agent.isStopped = false; return true; }
        }
        */
        return false; // None
        
    }
    public void Exit()
    {
    }
}

// WanderState : 이동만 담당, 도착하면 Idle 로 복귀

// DetectState
public sealed class MonsterDetectState : IMonsterState
{
    readonly MonsterContext ctx;
    readonly MonsterStateMachine machine;
    CancellationTokenSource cts;

    const float hearInterval = 0.5f;   // 청각 체크 주기
    const float chaseTimeout = 5f;     // 최근 소리 후 추적 유지 시간
    string _walkLoopClipKey;
    string _detectClipKey;
    float hearTimer;    // 0.5초 타이머
    float chaseTimer;   // 5초 타이머
    Vector3 targetPos;  // 마지막 들린 위치
    float returnGate;

    public MonsterDetectState(MonsterContext c, MonsterStateMachine m) { ctx = c; machine = m; }

    public async void Enter()
    {
        switch (ctx.data.category)
        {
            case MonsterData.MonsterCategory.Cleaner:
                _walkLoopClipKey = "SFX_CleanerWalk";
                break;
            case MonsterData.MonsterCategory.Hound:
                _walkLoopClipKey = "SFX_HoundWalk";
                break;
            case MonsterData.MonsterCategory.Beetle:
                _walkLoopClipKey = "SFX_BettleWalk"; // 요청대로 Bettle 표기 사용
                break;
            default:
                _walkLoopClipKey = null;
                break;
        }
        if (_walkLoopClipKey != null)
        {

            //SoundManager.Instance.
        }
        ctx.animationHub?.SetTag(MonsterStateTag.Detect, ctx);
        ctx.anim.Play("Walk");
        ctx.SightLocked = false;

        Vector2 dir = (ctx.player.position - ctx.transform.position).normalized;

        hearTimer = 0f;
        chaseTimer = chaseTimeout;
        returnGate = 0f;
    }
    public async void Tick()
    {
    }
    public void Exit()
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = null;

        if (ctx.alert) ctx.alert.gameObject.SetActive(false);
    }
    // ========== 내부 로직 ==========

}
public sealed class MonsterKilledState : IMonsterState
{
    static readonly HashSet<GameObject> s_despawning = new();
    static readonly Dictionary<GameObject, MonsterKilledState> s_instances = new();
    static readonly Dictionary<GameObject, int> s_versions = new();
    static readonly Dictionary<GameObject, int> s_harvestsLeft = new();
    int _ver = 0;
    bool _killedSfxPlayed;
    readonly MonsterContext ctx;
    readonly MonsterController ctr;
    readonly MonsterStateMachine root;
    readonly GameObject go;
    public static bool IsDespawning(GameObject go) => s_despawning.Contains(go);
    public static int HarvestsLeft(GameObject go) // 외부에서 갈무리 남은 횟수 조회용
    {
        if (go != null && s_harvestsLeft.TryGetValue(go, out var left)) return left;
        return 0;
    }
    public static void ResetAlphaOnSpawn(GameObject go)
    {
        if (!go) return;
        var srs = go.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in srs)
        {
            if (!sr) continue;
            var c = sr.color;
            if (c.a < 1f) sr.color = new Color(c.r, c.g, c.b, 1f);
        }
        if (!s_versions.TryGetValue(go, out var v)) v = 0;
        s_versions[go] = v + 1;
        s_despawning.Remove(go);
        s_instances.Remove(go);
        s_harvestsLeft.Remove(go);
    }
    public MonsterKilledState(MonsterContext c, MonsterStateMachine m, GameObject go, MonsterController mc)
    { ctx = c; root = m; this.go = go; ctr = mc; }

    public void Enter()
    {
        if (!_killedSfxPlayed)
        {
            string sfxName = null;
            switch (ctx.data.category)
            {
                case MonsterData.MonsterCategory.Cleaner:
                    sfxName = "SFX_CleanerDie";
                    break;
                case MonsterData.MonsterCategory.Hound:
                    sfxName = "SFX_HoundDie";
                    break;
                case MonsterData.MonsterCategory.Beetle:
                    sfxName = "SFX_BettleDie";
                    break;
                case MonsterData.MonsterCategory.Titan:
                    sfxName = "SFX_TitanDie";
                    break;
                default:
                    sfxName = "SFX_GenericDie";
                    break;
            }

        }

        ctr._killed = true;
        if (ctx.alert) ctx.alert.gameObject.SetActive(false);

        if (!s_versions.TryGetValue(go, out var v)) v = 0;
        _ver = s_versions[go] = v + 1;

        s_instances[go] = this;

        _isFading = false;

        s_harvestsLeft[go] = 2;
        ctx.animationHub?.SetTag(MonsterStateTag.Killed, ctx);

    }
    bool _isFading;

    // 바로 호출
    public void StartFadeNow()
    {
        if (!IsCurrentInstance() || !IsCurrentVersion()) return;
        if (_isFading) return;
        //FadeAndReleaseAsync(3f).Forget();
    }
    /*
    async UniTask FadeAndReleaseAsync(float fadeSeconds)
    {
        if (!IsCurrentInstance() || !IsCurrentVersion()) return;
        _isFading = true;
        s_despawning.Add(go);

        // 알파 서서히 0으로
        var srs = go.GetComponentsInChildren<SpriteRenderer>(true);
        var cache = new List<(SpriteRenderer sr, Color c)>(srs.Length);
        foreach (var sr in srs) if (sr) cache.Add((sr, sr.color));

        float t = 0f;
        while (t < fadeSeconds)
        {
            if (!IsCurrentInstance() || !IsCurrentVersion()) return;
            float a = Mathf.Lerp(1f, 0f, t / fadeSeconds);
            for (int i = 0; i < cache.Count; i++)
            {
                var (sr, c) = cache[i];
                if (sr) sr.color = new Color(c.r, c.g, c.b, a);
            }
            t += Time.deltaTime;
            await Cysharp.Threading.Tasks.UniTask.Yield();
        }
        for (int i = 0; i < cache.Count; i++)
        {
            var (sr, c) = cache[i];
            if (sr) sr.color = new Color(c.r, c.g, c.b, 0f);
        }

        if (!IsCurrentInstance() || !IsCurrentVersion()) return;
        // 정리
        s_despawning.Remove(go);
        s_instances.Remove(go);
        s_harvestsLeft.Remove(go);
    }
    */
    void GoVanish()
    {
        ctx.animationHub?.SetTag(MonsterStateTag.Killed, ctx);
        //VanishAndRelease().Forget();
    }

    bool IsCurrentInstance()
    {
        return s_instances.TryGetValue(go, out var inst) && ReferenceEquals(inst, this);
    }

    bool IsCurrentVersion()
    {
        return s_versions.TryGetValue(go, out var cur) && cur == _ver;
    }
    public void Tick() { }
    public void Exit() { }
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