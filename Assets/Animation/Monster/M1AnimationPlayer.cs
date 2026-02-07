using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class M1AnimationPlayer : AnimationPlayerBase
{
    [Header("Targets")]
    [SerializeField] Animator animator;
    [SerializeField] SpriteRenderer spriteRenderer; // 선택

    [Header("Use Direction?")]
    public bool useDirectionOn = false;

    [Header("Naming")]
    public string prefix = "bettle";
    public string idleKey = "idle";
    public string walkKey = "walk";
    public string runKey = "run";
    public string detectKey = "detect";
    public string hideKey = "hide";
    public string idleHideKey = "idlehide";

    [Header("Direction Keys (optional)")]
    public string frontKey = "front";
    public string backKey = "back";
    public string sideKey = "side";

    [Header("Flip Options")]
    [Tooltip("useDirectionOn=false일 때도 이동 방향(X)으로 flipX를 갱신합니다.")]
    public bool flipByForwardX = true;
    [Tooltip("flipX 판정에 사용할 최소 X속도(정지 떨림 방지).")]
    public float flipXEps = 0.01f;

    MonsterStateTag _tag = MonsterStateTag.None;
    MonsterContext _ctx;
    string _currentStateName;

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>(true);
        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
    }
#if UNITY_EDITOR
    void OnValidate()
    {
        if (!animator) animator = GetComponentInChildren<Animator>(true);
        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
    }
#endif

    public override bool IsActiveFor(MonsterContext ctx) => true;

    public override void SetTag(MonsterStateTag tag, MonsterContext ctx)
    {
        _tag = tag;
        _ctx = ctx;
        PlayNow();
    }

    void LateUpdate()
    {
        if (_tag != MonsterStateTag.None && _ctx != null)
            PlayNow();
    }

    void PlayNow()
    {
        if (!animator) return;

        string motion = ResolveMotion(_tag, _ctx);
        if (motion == null) return;

        // run 미보유 → walk 대체
        if (!HasState(SetString(motion)) && motion == runKey)
            motion = walkKey;

        string stateName;
        if (!useDirectionOn)
        {
            stateName = SetString(motion);                 // Bettle_idle / Bettler_walk / Bettle_run
            ApplyFlipByForwardX();
        }
        else
        {
            stateName = $"{prefix}_{motion}"; // Cleanerfront_walk
            if (!HasState(stateName))
                stateName = SetString(motion);             // 방향 클립 없으면 무시
            ApplyFlipByForwardX();
        }

        if (_currentStateName == stateName) return;
        animator.Play(stateName, 0, 0f);
        _currentStateName = stateName;
    }

    string ResolveMotion(MonsterStateTag tag, MonsterContext ctx)
    {
        switch (tag)
        {
            case MonsterStateTag.Idle: return idleHideKey;
            case MonsterStateTag.Stun: return idleKey;
            case MonsterStateTag.Detect:
            case MonsterStateTag.SearchWander:
            case MonsterStateTag.Trace: return walkKey;
            case MonsterStateTag.Return: return (ctx != null && ctx.IsFastReturn) ? runKey : walkKey;
            case MonsterStateTag.CombatMove:
            case MonsterStateTag.CombatAttack:
            case MonsterStateTag.Flee: return detectKey;
            case MonsterStateTag.Hide: return hideKey;
            case MonsterStateTag.Killed: return hideKey;
            default: return null;
        }
    }
    void ApplyFlipByForwardX()
    {
        if (!spriteRenderer || _ctx == null || !flipByForwardX) return;

    }
   

    string SetString(string motion) => $"{prefix}_{motion}";

    bool HasState(string stateName)
    {
        if (!animator) return false;
        int hash = Animator.StringToHash(stateName);
        return animator.HasState(0, hash);
    }
}