using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;   // Handles, Gizmos
#endif
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator), typeof(SpriteRenderer))]
public class MonsterController : MonoBehaviour
{
    [SerializeField] MonsterAnimationHub animationHub;
    public MonsterAnimationHub AnimationHub => animationHub;
    public SpriteRenderer AlertSR { get; private set; }
    [SerializeField] SpriteRenderer alertSR;
    [SerializeField] MonsterData data;
    [SerializeField] public Vector3 spawner;
    [SerializeField] LayerMask obstacleMask;
    [SerializeField] public string monster_Id;
    public LayerMask ObstacleMask => obstacleMask;
    // 캐시
    public MonsterData Data => data;
    public Animator Animator { get; private set; }
    public SpriteRenderer Sprite { get; private set; }
    public Vector3 Spawner => spawner;
    public Transform Player { get; private set; }
    public MonsterStateMachine StateMachine => root;
    public float MaxHP { get; private set; }
    public float CurrentHP { get; private set; }
    public event Action<float, float> OnHpChanged;
    public event Action<float> OnDamaged;

    MonsterStateMachine root = new();
    MonsterContext ctx;
    [Header("Z-Lock")]
    [SerializeField] bool lockZ = true;
    [SerializeField] float lockedZ = -0.0001f;

    bool _initialized = false;
    public bool _killed = false;
    void Awake()
    {
        Animator = GetComponent<Animator>();
        if (!animationHub)
            animationHub = GetComponentInChildren<MonsterAnimationHub>(true);
        Sprite = GetComponent<SpriteRenderer>();
        Player = GameObject.FindWithTag("Player")?.transform;


    }

    public void InitAfterSpawn(string monsterId)
    {
        if (string.IsNullOrEmpty(monsterId))
        {
            Debug.LogError($"[{name}] InitAfterSpawn: monsterId is null/empty");
            return;
        }
        if (Data == null)
        {
            Debug.LogError($"[{name}] MonsterData is not assigned on prefab/instance!");
            return;
        }

        ctx = new(this);
        ctx.hub.ResetAll();
        _killed = false;
        CurrentHP = MaxHP;
        ctx.hp = MaxHP;
        OnHpChanged?.Invoke(CurrentHP, MaxHP);

        root = new MonsterStateMachine();
        root.ChangeState(new MonsterIdleState(ctx, root));
        _initialized = true;
    }
    void OnDisable()
    {
        _initialized = false;           // 풀로 반환될 때 등, 안전 가드
    }

    void Start()
    {
    }

    void Update()
    {
        if (!_initialized) return;
        root.Tick();
    }
    void LateUpdate()
    {
        if (!_initialized) return;
        if (!lockZ) return;

        var p = transform.position;
        if (Mathf.Abs(p.z - lockedZ) > 1e-6f)
        {
            p.z = lockedZ;
            transform.position = p;

        }
    }

    #region 피격 시 깜빡거림
    Coroutine _damageBlinkCo;

    // MPB 헬퍼 (플레이어와 동일)
    static void SetSpriteAlpha(SpriteRenderer sr, float a)
    {
        if (!sr) return;
        var mpb = new MaterialPropertyBlock();
        sr.GetPropertyBlock(mpb);
        if (mpb == null) mpb = new MaterialPropertyBlock();

        Color baseColor = Color.white;
        if (sr.sharedMaterial && sr.sharedMaterial.HasProperty("_Color"))
            baseColor = sr.sharedMaterial.color;
        mpb.SetColor("_Color", new Color(baseColor.r, baseColor.g, baseColor.b, a));
        sr.SetPropertyBlock(mpb);
    }
    static void ResetSpriteAlpha(SpriteRenderer sr)
    {
        SetSpriteAlpha(sr, 1f);
    }

    IEnumerator DamageBlinkRoutine(float duration = 1f, float blinkInterval = 0.1f)
    {
        float t = 0f;
        bool low = false;
        const float lowA = 0.35f;

        while (t < duration)
        {
            low = !low;
            SetSpriteAlpha(Sprite, low ? lowA : 1f);
            yield return new WaitForSeconds(blinkInterval);
            t += blinkInterval;
        }
        ResetSpriteAlpha(Sprite);
        _damageBlinkCo = null;
    }
    #endregion
    // 외부에서 데미지
    // 기존 시그니처 보존
    public void TakeDamage(float dmg) => TakeDamage(dmg, 0f);

    // 새 시그니처 스턴 지속시간 포함
    public void TakeDamage(float dmg, float stunSec)
    {
        if (_killed)
            return;
        ctx.hp = Mathf.Max(0, ctx.hp - dmg);
        CurrentHP = ctx.hp;
        OnHpChanged?.Invoke(CurrentHP, MaxHP);
        OnDamaged?.Invoke(dmg);
        Debug.Log($"{monster_Id} 몬스터에게 {dmg} 피해! (stun={stunSec:F2}s)");

        /*
        string sfxName = null;
        switch (ctx.data.category)
        {
            case MonsterData.MonsterCategory.Cleaner:
                sfxName = "SFX_CleanerDamaged";
                break;
            case MonsterData.MonsterCategory.Hound:
                sfxName = "SFX_HoundDamaged";
                break;
            case MonsterData.MonsterCategory.Beetle:
                sfxName = "SFX_BettleDamaged";
                break;
            case MonsterData.MonsterCategory.Titan:
                sfxName = "SFX_TitanDamaged";
                break;
            default:
                sfxName = "SFX_GenericDamaged";
                break;
        }

        if (!string.IsNullOrEmpty(sfxName))
        {
            SoundManager.Instance.PlaySound3D(
                sfxName,
                transform,       // 이 몬스터의 위치에서
                0f,               // delay 없음
                false,            // isLoop = false (한 번만)
                SoundType.SFX,
                true,             // attachToTarget = true (몬스터 따라감)
                1.5f,             // minDistance
                25f               // maxDistance
            );
        }
        */
        if (_damageBlinkCo != null) StopCoroutine(_damageBlinkCo);
        _damageBlinkCo = StartCoroutine(DamageBlinkRoutine(1f, 0.1f));

        if (ctx.hp <= 0f)
        {
            StateMachine.ChangeState(new MonsterKilledState(ctx, StateMachine, gameObject, this));
            return;
        }
        
    }
}
