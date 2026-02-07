using UnityEngine;

[CreateAssetMenu(fileName = "MonsterData", menuName = "Mobs/Monster Data", order = 0)]
public class MonsterData : ScriptableObject
{
    [Header("공통 스탯")]
    public string monsterName = "Normal";
    public float maxHp = 100f;
    public float attackPower = 5f;
    public float attackCooldown = 1.5f;
    public float attackRange = 1.2f;

    [Header("픽셀 이동")]
    [Tooltip("스프라이트/월드 변환 기준 PPU")]
    public int pixelsPerUnit = 16;

    [Tooltip("초당 이동 픽셀 수")]
    public float moveSpeedPixelsPerSec = 40f;

    [Header("공격/이동 전환 거리")]
    public float attackEnterDistance = 2.2f;
    public float attackExitDistance = 3.0f;

    [Header("접근/ 이동 행동 리프 각각 구분해서 넣기")]
    public AttackBehaviourSO[] combatMoveBehaviours;
    public AttackBehaviourSO[] combatAttackBehaviours;
    public float[] moveWeights = { 60, 25, 15 };

    public enum MonsterCategory { Shrimp, Octopus, Salt, Scrubber, OiliveOil }
    [Header("행동 타입")]
    public MonsterCategory category = MonsterCategory.Shrimp;

    [Header("피격 스턴")]
    public float defaultHitStunSeconds = 0.3f;

    [Header("드롭")]
    [Tooltip("죽을 때 떨어뜨릴 재화 양(0이면 드롭 안 함)")]
    public int dropCurrencyAmount = 0;

    [Tooltip("재화 드롭 프리팹(예: 코인/젬). CurrencyPickup 컴포넌트 포함 필요")]
    public GameObject currencyDropPrefab;

    [Tooltip("죽을 때 사라지는(페이드) 시간")]
    public float deathFadeSeconds = 0.8f;

    [Header("몸박/근접 데미지")]
    [Tooltip("플레이어에게 데미지를 줄 수 있는 근접 사거리(월드 단위)")]
    public float contactDamageRange = 1.0f;

    [Tooltip("몸박 데미지 1틱당 피해량")]
    public float contactDamage = 5f;

    [Tooltip("몸박 데미지 간격(초). 예: 0.5면 0.5초마다 1번")]
    public float contactDamageInterval = 0.5f;

    [Tooltip("켜면 근접 사거리만 만족해도 데미지(거리 체크). 끄면 트리거/충돌 접촉이 있어야만 데미지")]
    public bool contactDamageByRange = true;

}
