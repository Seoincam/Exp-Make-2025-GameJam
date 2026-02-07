using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class SoySauceField : MonoBehaviour
{
    [Header("Runtime Spec")]
    [SerializeField] float duration = 10f;
    [SerializeField, Range(0f, 3f)] float speedBonus01 = 0.30f;

    [Header("Layer Filter")]
    [SerializeField] LayerMask monsterMask;
    [SerializeField] LayerMask playerMask;

    [Header("Player Damage (TODO Hook Only)")]
    [SerializeField] float playerTickInterval = 0.5f;
    [SerializeField] float playerTickDamage = 5f;

    [Header("Debug")]
    [SerializeField] bool log = false;

    readonly HashSet<MonsterController> _buffedMonsters = new();
    int _playerInsideCount = 0;
    Coroutine _playerTickCo;

    BoxCollider2D _trigger;

    // ====== 프리팹 없이 생성하는 팩토리 ======
    public static SoySauceField Spawn(
        Vector3 position,
        float duration,
        float speedBonus01,
        Vector2 size,
        LayerMask monsterMask,
        LayerMask playerMask,
        float playerTickInterval = 0.5f,
        float playerTickDamage = 5f,
        bool debugLog = false,
        Transform parent = null
    )
    {
        var go = new GameObject("SoySauceField_Runtime");
        if (parent) go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.rotation = Quaternion.identity;

        // 트리거 이벤트 보장용 Rigidbody2D
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        // 콜라이더
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(Mathf.Max(0.01f, size.x), Mathf.Max(0.01f, size.y));

        // 본체
        var field = go.AddComponent<SoySauceField>();
        field.duration = Mathf.Max(0.01f, duration);
        field.speedBonus01 = Mathf.Max(0f, speedBonus01);
        field.monsterMask = monsterMask;
        field.playerMask = playerMask;
        field.playerTickInterval = Mathf.Max(0.01f, playerTickInterval);
        field.playerTickDamage = Mathf.Max(0f, playerTickDamage);
        field.log = debugLog;
        field._trigger = box;

        // life
        Object.Destroy(go, field.duration);
        return field;
    }

    void Awake()
    {
        EnsureTrigger();
        // Spawn()로 만들면 이미 Destroy 예약이 걸려있지만, 혹시 수동 배치도 지원
        if (duration > 0f) Destroy(gameObject, duration);
    }

    void EnsureTrigger()
    {
        if (_trigger) return;
        _trigger = GetComponent<BoxCollider2D>();
        _trigger.isTrigger = true;
    }

    static bool IsInLayerMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[SoySauceField] ENTER: {other.name} layer={LayerMask.LayerToName(other.gameObject.layer)} rootLayer={LayerMask.LayerToName(other.transform.root.gameObject.layer)}");
    
    int layer = other.gameObject.layer;

        if (IsInLayerMask(layer, monsterMask))
        {
            var mc = other.GetComponentInParent<MonsterController>();
            if (mc && _buffedMonsters.Add(mc))
            {
                if (log) Debug.Log($"[SoySauceField] Buff ON: {mc.name}");
                mc.AddMoveSpeedMultiplierBuff(this, 1f + speedBonus01);
            }
            return;
        }

        if (IsInLayerMask(layer, playerMask))
        {
            _playerInsideCount++;
            if (log) Debug.Log($"[SoySauceField] Player enter. count={_playerInsideCount}");

            if (_playerTickCo == null)
                _playerTickCo = StartCoroutine(PlayerTickLoop());
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        int layer = other.gameObject.layer;

        if (IsInLayerMask(layer, monsterMask))
        {
            var mc = other.GetComponentInParent<MonsterController>();
            if (mc && _buffedMonsters.Remove(mc))
            {
                if (log) Debug.Log($"[SoySauceField] Buff OFF: {mc.name}");
                mc.RemoveMoveSpeedMultiplierBuff(this);
            }
            return;
        }

        if (IsInLayerMask(layer, playerMask))
        {
            _playerInsideCount = Mathf.Max(0, _playerInsideCount - 1);
            if (log) Debug.Log($"[SoySauceField] Player exit. count={_playerInsideCount}");

            if (_playerInsideCount == 0 && _playerTickCo != null)
            {
                StopCoroutine(_playerTickCo);
                _playerTickCo = null;
            }
        }
    }

    IEnumerator PlayerTickLoop()
    {
        while (_playerInsideCount > 0)
        {
            // TODO: 여기서 플레이어 틱 데미지 적용 직전 훅
            Debug.Log($"[SoySauceField] Player tick about to apply: {playerTickDamage}");
            yield return new WaitForSeconds(playerTickInterval);
        }
        _playerTickCo = null;
    }

    void OnDisable() => Cleanup();
    void OnDestroy() => Cleanup();

    void Cleanup()
    {
        foreach (var mc in _buffedMonsters)
            if (mc) mc.RemoveMoveSpeedMultiplierBuff(this);

        _buffedMonsters.Clear();

        if (_playerTickCo != null)
        {
            StopCoroutine(_playerTickCo);
            _playerTickCo = null;
        }
        _playerInsideCount = 0;
    }
}
