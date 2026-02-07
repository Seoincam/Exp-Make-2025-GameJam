using Combat.Shoot;
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
    public event Action<float, float> OnHpChanged;
    public event Action<float> OnDamaged;

    MonsterStateMachine root = new();
    MonsterContext ctx;

    bool _initialized = false;
    public bool _killed = false;

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
        // 스포너가 InitAfterSpawn를 확실히 호출한다면 이 블록은 제거
        if (!_initialized)
        {
            // monster_Id가 비어있으면 임시로 name 사용
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

        MaxHP = Data.maxHp;
        CurrentHP = MaxHP;
        OnHpChanged?.Invoke(CurrentHP, MaxHP);

        _killed = false;
        _deathSequenceStarted = false;
        if (_deathCo != null) { StopCoroutine(_deathCo); _deathCo = null; }

        // (풀링 쓰면 여기서 알파 복구도 해주는 게 좋음)
        ResetAllSpriteAlpha();

        SyncInspectorHP();
        ctx = new MonsterContext(this);
        ctx.hub.ResetAll();
        ctx.hp = MaxHP;

        root = new MonsterStateMachine();
        root.ChangeState(new MonsterDetectState(ctx, root));

        _initialized = true;
    }

    void OnDisable()
    {
        _initialized = false;
        if (_deathCo != null) { StopCoroutine(_deathCo); _deathCo = null; }
    }

    void Update()
    {
        if (Player == null)
            Player = GameObject.FindWithTag("Player")?.transform;
        inspectorInitialized = _initialized;
        inspectorHasData = (Data != null);
        inspectorHasPlayer = (Player != null);
        inspectorState = root != null && root.Current != null ? root.Current.GetType().Name : "None";

        if (!_initialized) return;
        root.Tick();
        TryApplyContactDamage(Time.time);
    }
    void TryApplyContactDamage(float now)
    {
        if (_killed) return;
        if (!_initialized) return;
        if (Data == null) return;
        if (Player == null) return;

        // 데미지 옵션이 꺼져있으면 스킵
        if (Data.contactDamage <= 0f) return;

        bool canHit = false;

        if (Data.contactDamageByRange)
        {
            float dist = Vector2.Distance(transform.position, Player.position);
            canHit = dist <= Data.contactDamageRange;
        }
        else
        {
            // 접촉(트리거/충돌) 기반
            canHit = _isTouchingPlayer;
        }

        inspectorPlayerInContact = canHit;

        if (!canHit) return;

        float interval = Mathf.Max(0.01f, Data.contactDamageInterval);
        if (now < _nextContactDamageTime) return;

        _nextContactDamageTime = now + interval;
        inspectorNextContactDamageTime = _nextContactDamageTime;

        ApplyDamageToPlayer(Data.contactDamage);
    }

    void ApplyDamageToPlayer(float amount)
    {
        Debug.Log($"플레이어에게 {amount}데미지");
        // 플레이어 근접시 데미지를 주는 로직입니다. 플레이어 로직이랑 연결부탁드리겟습니다.
    }
    public void Damage(DamageInfo damageInfo) => TakeDamage(damageInfo);

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_killed) return;

        var dmg = damageInfo.Damage;

        ctx.hp = Mathf.Max(0, ctx.hp - dmg);
        CurrentHP = ctx.hp;
        OnHpChanged?.Invoke(CurrentHP, MaxHP);
        OnDamaged?.Invoke(dmg);
        SyncInspectorHP();

        if (ctx.hp <= 0f)
        {
            StateMachine.ChangeState(new MonsterKilledState(ctx, StateMachine, gameObject, this));
            return;
        }
    }

    // 죽음 시퀀스: 드롭 + 페이드
    public void BeginDeathSequence()
    {
        if (_deathSequenceStarted) return;
        _deathSequenceStarted = true;

        // 드롭
        TryDropCurrency();

        // 페이드
        float fadeSec = (Data != null && Data.deathFadeSeconds > 0f) ? Data.deathFadeSeconds : 0.6f;
        _deathCo = StartCoroutine(FadeOutAndDestroyRoutine(fadeSec));
    }

    void TryDropCurrency()
    {
        if (Data == null) return;
        if (Data.dropCurrencyAmount <= 0) return;
        if (!Data.currencyDropPrefab) return;

        var go = Instantiate(Data.currencyDropPrefab, transform.position, Quaternion.identity);

        var pickup = go.GetComponent<CurrencyPickup>();
        if (pickup != null)
        {
            pickup.Init(Data.dropCurrencyAmount);
        }
        else
        {
            Debug.LogWarning($"[{name}] currencyDropPrefab에 CurrencyPickup이 없음: {Data.currencyDropPrefab.name}");
        }
    }

    IEnumerator FadeOutAndDestroyRoutine(float seconds)
    {
        _killed = true;
        _initialized = false;

        var srs = GetComponentsInChildren<SpriteRenderer>(true);
        // 원래 색 캐시
        var baseColors = new Color[srs.Length];
        for (int i = 0; i < srs.Length; i++)
            baseColors[i] = srs[i] ? srs[i].color : Color.white;

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

        Destroy(gameObject); // 풀링이면 SetActive(false)로 교체
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
            if (c.a < 1f) sr.color = new Color(c.r, c.g, c.b, 1f);
        }
    }
}
