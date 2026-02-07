using Shared.Stat;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public class MonsterController : MonoBehaviour, IDamagable
{
    [SerializeField] MonsterAnimationHub animationHub;
    public MonsterAnimationHub AnimationHub => animationHub;

    [Header("=== Runtime HP (Debug) ===")]
    [SerializeField] float inspectorCurrentHP;
    [SerializeField] float inspectorMaxHP;
    [SerializeField] float inspectorHPRatio;

    [Header("=== Runtime Init (Debug) ===")]
    [SerializeField] bool inspectorInitialized;
    [SerializeField] bool inspectorHasData;
    [SerializeField] bool inspectorHasPlayer;
    [SerializeField] string inspectorState;

    [Header("=== Contact Damage (Runtime Debug) ===")]
    [SerializeField] bool inspectorPlayerInContact;
    [SerializeField] float inspectorNextContactDamageTime;

    [Header("=== Runtime Stat/Effect ===")]
    [SerializeField] Stat stat;
    [SerializeField] EffectManager effectManager;

    [Header("=== Damage Debug ===")]
    [SerializeField] bool logDamageReceived = true;


    float _nextContactDamageTime;
    bool _isTouchingPlayer;

    [SerializeField] MonsterData data;
    [SerializeField] public string monster_Id;

    public MonsterData Data => data;
    public Animator Animator { get; private set; }
    public SpriteRenderer Sprite { get; private set; }
    public Transform Player { get; private set; }
    public AttackTelegraphRect Telegraph { get; private set; }
    public MonsterStateMachine StateMachine => root;

    Dictionary<object, float> _moveSpeedMultipliersBySource;
    float _cachedFieldMultiplier = 1f;

    public float MaxHP { get; private set; }
    public float CurrentHP { get; private set; }
    public float CurrentMoveSpeedPixelsPerSec
    {
        get
        {
            float baseSpeed =
                stat != null
                    ? Mathf.Max(0f, stat.GetFinalValue(StatType.MoveSpeed))
                    : (Data != null ? Mathf.Max(0f, Data.moveSpeedPixelsPerSec) : 0f);

            return baseSpeed * _cachedFieldMultiplier;
        }
    }
    public event Action<float, float> OnHpChanged;
    public event Action<float> OnDamaged;

    MonsterStateMachine root = new();
    MonsterContext ctx;

    bool _initialized;
    public bool _killed;

    bool _deathSequenceStarted;
    Coroutine _deathCo;

    void Awake()
    {
        Animator = GetComponent<Animator>();
        if (!animationHub)
            animationHub = GetComponentInChildren<MonsterAnimationHub>(true);

        Sprite = GetComponent<SpriteRenderer>();
        Player = GameObject.FindWithTag("Player")?.transform;
    }

    void Start()
    {
        if (!_initialized)
        {
            InitAfterSpawn(string.IsNullOrEmpty(monster_Id) ? name : monster_Id);
        }
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

        monster_Id = monsterId;

        InitializeStatAndEffect();

        _killed = false;
        _deathSequenceStarted = false;
        if (_deathCo != null)
        {
            StopCoroutine(_deathCo);
            _deathCo = null;
        }

        ResetAllSpriteAlpha();

        ctx = new MonsterContext(this);
        ctx.hub.ResetAll();

        root = new MonsterStateMachine();
        root.ChangeState(new MonsterDetectState(ctx, root));

        _initialized = true;
        SyncInspectorHP();
    }

    void InitializeStatAndEffect()
    {
        float initialHp = Mathf.Max(1f, Data.maxHp);
        float initialMoveSpeed = Mathf.Max(0f, Data.moveSpeedPixelsPerSec);
        float initialFireInterval = Mathf.Max(0.01f, Data.attackCooldown);

        stat = new Stat(new[]
        {
            new Stat.InitialEntry(StatType.Health, initialHp),
            new Stat.InitialEntry(StatType.MoveSpeed, initialMoveSpeed),
            new Stat.InitialEntry(StatType.FireInterval, initialFireInterval),
        });

        effectManager = new EffectManager(stat);

        MaxHP = initialHp;
        CurrentHP = initialHp;
        OnHpChanged?.Invoke(CurrentHP, MaxHP);
    }
    public void EnsureTelegraph()
    {
        if (Telegraph == null) Telegraph = new AttackTelegraphRect();
        Telegraph.Ensure($"{name}_Telegraph", sortingOrder: 1000);
    }
    void OnDisable()
    {
        _initialized = false;
        if (_deathCo != null)
        {
            StopCoroutine(_deathCo);
            _deathCo = null;
        }
        Telegraph?.Hide();
    }

    void Update()
    {
        if (Player == null)
            Player = GameObject.FindWithTag("Player")?.transform;

        inspectorInitialized = _initialized;
        inspectorHasData = Data != null;
        inspectorHasPlayer = Player != null;
        inspectorState = root != null && root.Current != null ? root.Current.GetType().Name : "None";

        if (!_initialized)
            return;

        effectManager?.Tick(Time.deltaTime);
        SyncCurrentHpFromStat(true);

        if (!_killed && CurrentHP <= 0f)
        {
            StateMachine.ChangeState(new MonsterKilledState(ctx, StateMachine, gameObject, this));
            return;
        }

        root.Tick();
        TryApplyContactDamage(Time.time);
    }

    void TryApplyContactDamage(float now)
    {
        if (_killed || !_initialized || Data == null || Player == null)
            return;

        if (Data.contactDamage <= 0f)
            return;

        bool canHit;
        if (Data.contactDamageByRange)
        {
            float dist = Vector2.Distance(transform.position, Player.position);
            canHit = dist <= Data.contactDamageRange;
        }
        else
        {
            canHit = _isTouchingPlayer;
        }

        inspectorPlayerInContact = canHit;
        if (!canHit)
            return;

        float interval = Mathf.Max(0.01f, Data.contactDamageInterval);
        if (now < _nextContactDamageTime)
            return;

        _nextContactDamageTime = now + interval;
        inspectorNextContactDamageTime = _nextContactDamageTime;

        ApplyDamageToPlayer(Data.contactDamage);
    }

    void ApplyDamageToPlayer(float amount)
    {
        if (!Player)
            return;

        if (Player.TryGetComponent<IDamagable>(out var damageable))
        {
            damageable.Damage(new DamageInfo(amount, gameObject, EDamageType.Normal));
        }
    }

    public void Damage(DamageInfo damageInfo) => TakeDamage(damageInfo);

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_killed || !_initialized || stat == null)
            return;

        int appliedDamageInt = Mathf.CeilToInt(damageInfo.Damage);
        if (appliedDamageInt <= 0)
            return;

        float dmg = appliedDamageInt;
        float prevHp = stat.GetBaseValue(StatType.Health);
        float nextHp = Mathf.Max(0f, prevHp - dmg);
        stat.SetBaseValue(StatType.Health, nextHp);
        stat.ApplyPendingChanges();

        this.ShowDamagePopup(appliedDamageInt);

        if (logDamageReceived)
        {
            string sourceName = damageInfo.Source ? damageInfo.Source.name : "Unknown";
            Debug.Log($"[MonsterDamage][{name}] -{appliedDamageInt} from={sourceName} type={damageInfo.DamageType} flags={damageInfo.EffectFlags} HP {prevHp:0.##}->{nextHp:0.##}");
        }

        OnDamaged?.Invoke(dmg);

        SyncCurrentHpFromStat(true);

        if (CurrentHP <= 0f)
        {
            StateMachine.ChangeState(new MonsterKilledState(ctx, StateMachine, gameObject, this));
        }
    }


    public void BeginDeathSequence()
    {
        if (_deathSequenceStarted)
            return;

        _deathSequenceStarted = true;

        TryDropCurrency();

        float fadeSec = (Data != null && Data.deathFadeSeconds > 0f) ? Data.deathFadeSeconds : 0.6f;
        _deathCo = StartCoroutine(FadeOutAndDestroyRoutine(fadeSec));
    }

    void TryDropCurrency()
    {
        if (Data == null || Data.dropCurrencyAmount <= 0 || !Data.currencyDropPrefab)
            return;

        var go = Instantiate(Data.currencyDropPrefab, transform.position, Quaternion.identity);
        var pickup = go.GetComponent<CurrencyPickup>();

        if (pickup != null)
        {
            pickup.Init(Data.dropCurrencyAmount);
        }
        else
        {
            Debug.LogWarning($"[{name}] currencyDropPrefab has no CurrencyPickup: {Data.currencyDropPrefab.name}");
        }
    }

    IEnumerator FadeOutAndDestroyRoutine(float seconds)
    {
        _killed = true;
        _initialized = false;

        var srs = GetComponentsInChildren<SpriteRenderer>(true);
        var baseColors = new Color[srs.Length];
        for (int i = 0; i < srs.Length; i++)
        {
            baseColors[i] = srs[i] ? srs[i].color : Color.white;
        }

        float t = 0f;
        while (t < seconds)
        {
            float a = Mathf.Lerp(1f, 0f, t / seconds);
            for (int i = 0; i < srs.Length; i++)
            {
                var sr = srs[i];
                if (!sr) continue;

                var c = baseColors[i];
                sr.color = new Color(c.r, c.g, c.b, a);
            }

            t += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < srs.Length; i++)
        {
            var sr = srs[i];
            if (!sr) continue;

            var c = baseColors[i];
            sr.color = new Color(c.r, c.g, c.b, 0f);
        }

        Destroy(gameObject);
    }

    void SyncCurrentHpFromStat(bool notify)
    {
        if (stat == null)
            return;

        float previous = CurrentHP;
        CurrentHP = Mathf.Clamp(stat.GetFinalValue(StatType.Health), 0f, MaxHP);

        if (notify && !Mathf.Approximately(previous, CurrentHP))
        {
            OnHpChanged?.Invoke(CurrentHP, MaxHP);
        }

        SyncInspectorHP();
    }

    void SyncInspectorHP()
    {
        inspectorCurrentHP = CurrentHP;
        inspectorMaxHP = MaxHP;
        inspectorHPRatio = MaxHP > 0f ? CurrentHP / MaxHP : 0f;
    }

    void ResetAllSpriteAlpha()
    {
        var srs = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in srs)
        {
            if (!sr) continue;

            var c = sr.color;
            if (c.a < 1f)
            {
                sr.color = new Color(c.r, c.g, c.b, 1f);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _isTouchingPlayer = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _isTouchingPlayer = false;
        }
    }

    public void AddMoveSpeedMultiplierBuff(object source, float multiplier)
    {
        if (source == null) return;
        multiplier = Mathf.Max(0f, multiplier);

        _moveSpeedMultipliersBySource ??= new Dictionary<object, float>();
        _moveSpeedMultipliersBySource[source] = multiplier;

        RecomputeMoveSpeedMultiplier();

        Debug.Log($"[MoveSpeedBuff] {name} mult={_cachedFieldMultiplier:0.###} finalSpeed={CurrentMoveSpeedPixelsPerSec:0.###}");
    }

    public void RemoveMoveSpeedMultiplierBuff(object source)
    {
        if (_moveSpeedMultipliersBySource == null) return;
        if (!_moveSpeedMultipliersBySource.Remove(source)) return;

        if (_moveSpeedMultipliersBySource.Count == 0)
            _moveSpeedMultipliersBySource = null;

        RecomputeMoveSpeedMultiplier();

        Debug.Log($"[MoveSpeedBuff] {name} mult={_cachedFieldMultiplier:0.###} finalSpeed={CurrentMoveSpeedPixelsPerSec:0.###}");
    }


    void RecomputeMoveSpeedMultiplier()
    {
        float total = 1f;
        if (_moveSpeedMultipliersBySource != null)
        {
            foreach (var kv in _moveSpeedMultipliersBySource)
                total *= kv.Value; // 여러 장판 겹치면 곱(원하면 합산으로 바꿀 수도 있음)
        }
        _cachedFieldMultiplier = total;
    }

}
