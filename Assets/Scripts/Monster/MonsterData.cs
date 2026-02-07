using UnityEngine;

[CreateAssetMenu(fileName = "MonsterData",
                 menuName = "Mobs/Monster Data",
                 order = 0)]
public class MonsterData : ScriptableObject
{
    [Header("공통 스탯")]
    public string monsterName = "Normal";
    public float maxHp = 100f;
    public float attackPower = 5f;   // 1타 데미지
    public float attackCooldown = 1.5f; // 타격 간격
    public float attackRange = 1.2f; // 실제 타격 거리

    [Tooltip("플레이어와 유지하고 싶은 목표 거리")]
    public float traceDesiredDistance = 4f;
    [Tooltip("이 거리보다 가까우면 살짝 벌어짐(정지 밴드 하한)")]
    public float traceNearDistance = 2.5f;
    [Tooltip("이 거리보다 멀면 다가감(정지 밴드 상한)")]
    public float traceFarDistance = 5.5f;

    [Header("몬스터 피격 시 스턴 지속시간")]
    public float defaultHitStunSeconds = 0.3f;


    [Header("이 몬스터가 사용할 공격 패턴들 (순서대로 BT Selector)")]
    [Tooltip("이 이내면 공격 우선")]
    public float attackEnterDistance = 2.2f;
    [Tooltip("이 밖이면 이동 우선(히스테리시스)")]
    public float attackExitDistance = 3.0f;


    [Header("접근/ 이동 행동 리프 각각 구분해서 넣기")]
    public AttackBehaviourSO[] combatMoveBehaviours;   // 접근
    public AttackBehaviourSO[] combatAttackBehaviours; // 근접공격
    public float[] moveWeights = { 60, 25, 15 };

    public enum MonsterCategory { Hound, Turret, Miner, Beetle, Cleaner, Titan }
    [Header("행동 타입")]
    public MonsterCategory category = MonsterCategory.Hound;


}