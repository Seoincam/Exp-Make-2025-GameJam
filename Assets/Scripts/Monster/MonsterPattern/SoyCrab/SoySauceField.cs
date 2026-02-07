using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class SoySauceField : MonoBehaviour
{
    [Header("Runtime Spec")]
    [SerializeField] float duration = 10f;
    [SerializeField, Range(0f, 3f)] float speedBonus01 = 0.30f;

    // 장판의 실제 영역은 BoxCollider2D가 담당 (width/height 수동 스캔 제거)

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

    public void Init(float duration, float speedBonus01, Vector2 size)
    {
        this.duration = Mathf.Max(0.01f, duration);
        this.speedBonus01 = Mathf.Max(0f, speedBonus01);

        EnsureTrigger();
        _trigger.size = new Vector2(Mathf.Max(0.01f, size.x), Mathf.Max(0.01f, size.y));

        Destroy(gameObject, this.duration);
    }

    void Awake()
    {
        EnsureTrigger();
        Destroy(gameObject, duration);
    }

    void EnsureTrigger()
    {
        if (_trigger) return;

        _trigger = GetComponent<BoxCollider2D>();
        _trigger.isTrigger = true;

        // 오브젝트 스케일로 콜라이더가 비정상적으로 찌그러지는 걸 방지하려면
        // 프리팹의 transform scale을 (1,1,1)로 두는 걸 권장.
    }

    static bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        int layer = other.gameObject.layer;

        // ---- Monster ----
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

        // ---- Player ----
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

        // ---- Monster ----
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

        // ---- Player ----
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
        // 첫 틱을 즉시 주고 싶으면 yield return null 제거
        while (_playerInsideCount > 0)
        {
            // TODO: 여기서 플레이어 틱 데미지 적용 직전 훅
            Debug.Log($"[SoySauceField] Player tick about to apply: {playerTickDamage}");

            yield return new WaitForSeconds(Mathf.Max(0.01f, playerTickInterval));
        }

        _playerTickCo = null;
    }

    void OnDisable()
    {
        Cleanup();
    }

    void OnDestroy()
    {
        Cleanup();
    }

    void Cleanup()
    {
        foreach (var mc in _buffedMonsters)
        {
            if (mc) mc.RemoveMoveSpeedMultiplierBuff(this);
        }
        _buffedMonsters.Clear();

        if (_playerTickCo != null)
        {
            StopCoroutine(_playerTickCo);
            _playerTickCo = null;
        }
        _playerInsideCount = 0;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        var box = GetComponent<BoxCollider2D>();
        if (!box) return;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(box.offset, box.size);
    }
#endif
}
