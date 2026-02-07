using System.Collections.Generic;
using UnityEngine;

public sealed class MonsterContext
{
    public readonly MonsterData data;
    public readonly Transform transform;
    public readonly Animator anim;
    public readonly MonsterAnimationHub animationHub;
    public readonly SpriteRenderer sr;

    public MonsterDecisionHub hub { get; private set; }

    public readonly Transform player;
    public readonly MonsterController mono;
    public readonly MonsterStateMachine sm;

    public float hp;
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

        player = owner.Player;

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

        float stepUnits = (data.moveSpeedPixelsPerSec * UnitPerPixel) * dt;

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

    Vector3 SnapToPixelGrid(Vector2 pos)
    {
        float ppu = Mathf.Max(1, data.pixelsPerUnit);
        float x = Mathf.Round(pos.x * ppu) / ppu;
        float y = Mathf.Round(pos.y * ppu) / ppu;
        return new Vector3(x, y, transform.position.z);
    }
}
