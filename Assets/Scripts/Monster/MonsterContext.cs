using System.Collections.Generic;
using UnityEngine;

public sealed class MonsterContext
{
    public readonly MonsterData data;
    public readonly Transform transform;
    public readonly Animator anim;
    public readonly MonsterAnimationHub animationHub;
    public readonly SpriteRenderer sr;
    public Transform player => mono.Player;

    public MonsterDecisionHub hub { get; private set; }

    public readonly MonsterController mono;
    public readonly MonsterStateMachine sm;

    public bool MoveLocked { get; private set; }

    public void LockMove() => MoveLocked = true;
    public void UnlockMove() => MoveLocked = false;

    public int nextBehaviourIndex = -1;
    public bool isCombat;
    public bool isMoveState;

    public readonly Dictionary<IMonsterBehaviour, float> nextReadyTime = new();

    public void SetCooldown(IMonsterBehaviour beh, float cd)
    {
        if (!nextReadyTime.ContainsKey(beh)) nextReadyTime[beh] = 0f;
        nextReadyTime[beh] = Time.time + Mathf.Max(0f, cd);
    }

    public bool IsReady(IMonsterBehaviour beh)
        => !nextReadyTime.TryGetValue(beh, out var t) || Time.time >= t;

    Vector2 _realPos;
    bool _realPosInitialized;
    float UnitPerPixel => 1f / Mathf.Max(1, data.pixelsPerUnit);

    public MonsterContext(MonsterController owner)
    {
        mono = owner;
        sm = owner.StateMachine;

        data = owner.Data;
        transform = owner.transform;
        anim = owner.Animator;
        sr = owner.Sprite;
        animationHub = owner.AnimationHub;


        hub = new MonsterDecisionHub(this);
    }
    public void EnsureRealPos()
    {
        if (_realPosInitialized) return;
        _realPos = transform.position;
        _realPosInitialized = true;
    }

    public void MoveTowardPlayerPixelSmooth(float dt)
    {
        if (!player) return;

        EnsureRealPos();

        Vector2 dir = ((Vector2)player.position - _realPos);
        float dist = dir.magnitude;
        if (dist < 0.0001f) return;

        float stepUnits = (mono.CurrentMoveSpeedPixelsPerSec * UnitPerPixel) * dt;

        // 실제 위치는 연속적으로 누적
        _realPos += dir.normalized * stepUnits;

        // 표시만 픽셀 스냅
        transform.position = SnapToPixelGrid(_realPos);
    }

    public void StopMovePixel()
    {
        // 멈출 때 realPos도 현재 스냅 위치로 맞춰서 drift 방지
        _realPos = transform.position;
        _realPosInitialized = true;
    }
    static readonly Collider2D[] _hits = new Collider2D[16];

    public void MoveTowardPlayer_KeepDistance(float dt)
    {
        if (!player) return;

        EnsureRealPos();

        Vector2 pos = _realPos;

        float stepUnits = (mono.CurrentMoveSpeedPixelsPerSec * UnitPerPixel) * dt;

        float minP = Mathf.Max(0f, data.minPlayerDistance);
        Vector2 toPlayer = (Vector2)player.position - pos;
        float distP = toPlayer.magnitude;

        // distP가 minPlayerDistance보다 가까우면 플레이어에게서 멀어짐
        if (minP > 0f && distP > 0.0001f && distP < minP)
        {
            Vector2 awayFromPlayer = (-toPlayer / distP); // 플레이어 반대
            _realPos += awayFromPlayer * stepUnits;
            transform.position = SnapToPixelGrid(_realPos);
            return;
        }

        float r = Mathf.Max(0f, data.minMonsterDistance);
        if (r > 0f)
        {
            int count = Physics2D.OverlapCircleNonAlloc(pos, r, _hits, data.monsterLayer);

            Vector2 push = Vector2.zero;
            int used = 0;

            for (int i = 0; i < count; i++)
            {
                var c = _hits[i];
                if (!c) continue;
                if (c.transform == transform) continue;

                Vector2 other = c.transform.position;
                Vector2 away = pos - other;
                float d = away.magnitude;
                if (d < 0.0001f) continue;

                float overlap = (r - d);
                if (overlap > 0f)
                {
                    push += (away / d) * overlap; // 겹친 양만큼
                    used++;
                }
            }

            // 가까운 몬스터가 있으면 일단 벌어짐
            if (used > 0)
            {
                Vector2 dir = push.normalized;
                _realPos += dir * stepUnits;
                transform.position = SnapToPixelGrid(_realPos);
                return;
            }
        }

        if (toPlayer.sqrMagnitude < 0.0001f) return;
        _realPos += toPlayer.normalized * stepUnits;
        transform.position = SnapToPixelGrid(_realPos);
    }
    Vector3 SnapToPixelGrid(Vector2 pos)
    {
        float ppu = Mathf.Max(1, data.pixelsPerUnit);
        float x = Mathf.Round(pos.x * ppu) / ppu;
        float y = Mathf.Round(pos.y * ppu) / ppu;
        return new Vector3(x, y, transform.position.z);
    }
}
