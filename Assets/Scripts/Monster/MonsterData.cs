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


    public enum MonsterCategory { Hound, Turret, Miner, Beetle, Cleaner, Titan }
    [Header("행동 타입")]
    public MonsterCategory category = MonsterCategory.Hound;


}