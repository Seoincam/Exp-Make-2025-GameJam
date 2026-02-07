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

    public bool SightLocked { get; set; }

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

    public void MoveTowardPlayerPixel(float dt)
    {
        if (!player) return;

        Vector2 dir = (player.position - transform.position);
        if (dir.sqrMagnitude < 0.0001f) return;

        float stepUnits = (data.moveSpeedPixelsPerSec * UnitPerPixel) * dt;
        Vector2 next = (Vector2)transform.position + dir.normalized * stepUnits;

        // 픽셀 그리드로 스냅
        transform.position = SnapToPixelGrid(next);
    }

    public void StopMovePixel()
    {
        // velocity 기반이 아니라 별도 처리 없음.
    }

    Vector3 SnapToPixelGrid(Vector2 pos)
    {
        float ppu = Mathf.Max(1, data.pixelsPerUnit);
        float x = Mathf.Round(pos.x * ppu) / ppu;
        float y = Mathf.Round(pos.y * ppu) / ppu;
        return new Vector3(x, y, transform.position.z);
    }
}
