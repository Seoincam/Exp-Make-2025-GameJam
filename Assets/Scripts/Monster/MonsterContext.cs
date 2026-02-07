using System;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.AI;

public sealed class MonsterContext
{
    public readonly MonsterData data;
    public readonly Transform transform;
    public readonly Animator anim;
    public readonly MonsterAnimationHub animationHub;
    public readonly SpriteRenderer sr;
    //public readonly Vector3 spawner;
    public Vector3 spawner => mono.Spawner;
    public MonsterDecisionHub hub { get; private set; }
    public readonly SpriteRenderer alert;
    public readonly Transform player;
    public readonly LayerMask obstacleMask;
    public readonly MonsterController mono;
    public readonly MonsterStateMachine sm;
    public readonly string[] interestTags;
    public bool isaggressive;
    public int rank;
    public float hp;
    public float attack;
    public float hearRange;
    public float sightDistance;
    public bool SightLocked { get; set; }
    public float speed;
    public Vector3 LastHeardPos;
    public bool IsFastReturn;
    public string id;
    public int nextBehaviourIndex = -1;
    public bool isCombat;   // 공격 묶음 우선 선택 신호
    public bool isMoveState;     // 이동(접근/오빗 등) 묶음 우선 선택 신호
    Vector2 _lastForward = Vector2.right;
    public int patternCount;                 // 현재 누적 공격 횟수
    public int PatternEveryRest = 3;         // n회마다 쉬기(인스펙터에서 각 몬스터 SO로 제어해도 됨)
    public void IncPattern() => patternCount = Mathf.Min(patternCount + 1, 999);
    public void ResetPattern() => patternCount = 0;

    // 각 행동 쿨다운 관리용
    public readonly Dictionary<IMonsterBehaviour, float> nextReadyTime = new();
    public void SetCooldown(IMonsterBehaviour beh, float cd)
    {
        if (!nextReadyTime.ContainsKey(beh)) nextReadyTime[beh] = 0f;
        nextReadyTime[beh] = Time.time + Mathf.Max(0f, cd);
    }
    public bool IsReady(IMonsterBehaviour beh)
        => !nextReadyTime.TryGetValue(beh, out var t) || Time.time >= t;


    public MonsterContext(MonsterController owner)
    {
        animationHub = owner.AnimationHub;
        mono = owner;
        sm = owner.StateMachine;
        data = owner.Data;
        transform = owner.transform;
        anim = owner.Animator;
        sr = owner.Sprite;
        //spawner = owner.Spawner;
        alert = owner.AlertSR;
        player = owner.Player;

        hub = new MonsterDecisionHub(this);
        obstacleMask = owner.ObstacleMask;
    }

    #region 몬스터 시야 / 청각 탐지 로직
    // 시야 확인 (벽 Raycast 포함)
    public bool CanSeePlayer(float maxDist, float fovAngleDeg)
    {
        if (!player) return false;
        if (SightLocked) return false;

        Vector2 start = transform.position;
        Vector2 dir = (player.position - transform.position);
        float dist = dir.magnitude;
        if (dist > maxDist) return false;


        float halfAngle = fovAngleDeg * 0.5f;


        return true;
    }
  
    #endregion
    #region 네브메쉬 안전로직
   
    #endregion
}