using Shared.Stat;
using System;
using System.Collections;
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

    [Header("=== Damage Effect Settings ===")]
    [SerializeField] float slowDuration = 1.25f;
    [SerializeField, Range(0.05f, 1f)] float slowMultiplier = 0.6f;
    [SerializeField] float stunDuration = 0.35f;
    [SerializeField] float freezeDuration = 0.6f;

    float _nextContactDamageTime;
    bool _isTouchingPlayer;

    [SerializeField] MonsterData data;
    [SerializeField] public string monster_Id;

    public MonsterData Data => data;
    public Animator Animator { get; private set; }
    public SpriteRenderer Sprite { get; private set; }
    public Transform Player { get; private set; }
    public MonsterStateMachine StateMachine => root;

    public float MaxHP { get; private set; }
    public float CurrentHP { get; private set; }
    public float CurrentMoveSpeedPixelsPerSec
        => stat != null
            ? Mathf.Max(0f, stat.GetFinalValue(StatType.MoveSpeed))
            : (Data != null ? Mathf.Max(0f, Data.moveSpeedPixelsPerSec) : 0f);

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

    void OnDisable()
    {
        _initialized = false;
        if (_deathCo != null)
        {
            StopCoroutine(_deathCo);
            _deathCo = null;
        }
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

        float dmg = Mathf.Max(0f, damageInfo.Damage);
        if (dmg <= 0f)
            return;

        float nextHp = Mathf.Max(0f, stat.GetBaseValue(StatType.Health) - dmg);
        stat.SetBaseValue(StatType.Health, nextHp);
        stat.ApplyPendingChanges();

        ApplyDamageEffects(damageInfo);
        OnDamaged?.Invoke(dmg);

        SyncCurrentHpFromStat(true);

        if (CurrentHP <= 0f)
        {
            StateMachine.ChangeState(new MonsterKilledState(ctx, StateMachine, gameObject, this));
        }
    }

    void ApplyDamageEffects(in DamageInfo damageInfo)
    {
        if (effectManager == null)
            return;

        var flags = damageInfo.EffectFlags;

        if ((flags & EDamageEffectFlags.Slow) != 0)
        {
            var slowSpec = Effect.CreateSpec(EffectType.Slow)
                .SetUnique()
                .AddHandler(new LifeTimeHandler(Mathf.Max(0.01f, slowDuration)))
                .AddHandler(new TemporaryModifierHandler(StatType.MoveSpeed, ModifierType.Multiplicative, slowMultiplier));
            effectManager.AddEffect(slowSpec);
        }

        if ((flags & EDamageEffectFlags.Stun) != 0)
        {
            var stunSpec = Effect.CreateSpec(EffectType.Stun)
                .SetUnique()
                .AddHandler(new LifeTimeHandler(Mathf.Max(0.01f, stunDuration)))
                .AddHandler(new TemporaryModifierHandler(StatType.MoveSpeed, ModifierType.Override, 0f));
            effectManager.AddEffect(stunSpec);
        }

        if ((flags & EDamageEffectFlags.Freeze) != 0)
        {
            var freezeSpec = Effect.CreateSpec(EffectType.Freeze)
                .SetUnique()
                .AddHandler(new LifeTimeHandler(Mathf.Max(0.01f, freezeDuration)))
                .AddHandler(new TemporaryModifierHandler(StatType.MoveSpeed, ModifierType.Override, 0f));
            effectManager.AddEffect(freezeSpec);
        }

        // Apply queued final-value changes caused by newly added effects immediately.
        effectManager.Tick(0f);
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
}
