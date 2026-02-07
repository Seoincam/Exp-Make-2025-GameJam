using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어 기준으로 "일정 거리(사각 범위) 밖"에서 스폰.
/// - 웨이브: 총 spawnCountPerWave 마리를 스폰
/// - 배치(버스트): spawnBatchSize 마리씩 한 번에 스폰, 배치 간 간격 spawnIntervalSeconds
/// - 프리팹 3종 가중치 랜덤 선택
/// </summary>
public class WeightedOutOfRangeSpawner : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Wave Settings")]
    [Min(1)]
    [SerializeField] private int spawnCountPerWave = 12;

    [Tooltip("배치(버스트) 사이의 간격(초)")]
    [Min(0f)]
    [SerializeField] private float spawnIntervalSeconds = 1f;

    [Tooltip("한 번(한 배치)에 몇 마리를 동시에 스폰할지")]
    [Min(1)]
    [SerializeField] private int spawnBatchSize = 3;

    [Header("Out-of-Range Box (Player Centered)")]
    [Min(0f)]
    [SerializeField] private float noSpawnHalfWidthX = 10f;

    [Min(0f)]
    [SerializeField] private float noSpawnHalfHeightY = 6f;

    [Header("Spawn Area (Player Centered)")]
    [Min(0f)]
    [SerializeField] private float spawnAreaHalfWidthX = 25f;

    [Min(0f)]
    [SerializeField] private float spawnAreaHalfHeightY = 15f;

    [Header("Spawn Placement")]
    [SerializeField] private float spawnZ = 0f;

    [Range(1, 50)]
    [SerializeField] private int maxPositionTries = 15;

    [Tooltip("한 배치 내에서 여러 마리가 너무 겹치지 않게 최소 거리(0이면 제한 없음)")]
    [Min(0f)]
    [SerializeField] private float minSeparationInBatch = 0.5f;

    [SerializeField] private Transform spawnParent;

    [Header("Weighted Prefabs (3 Types)")]
    [SerializeField] private WeightedPrefab a;
    [SerializeField] private WeightedPrefab b;
    [SerializeField] private WeightedPrefab c;

    [Header("Auto Start")]
    [SerializeField] private bool startOnEnable = true;

    private Coroutine _running;

    [Serializable]
    private struct WeightedPrefab
    {
        public GameObject prefab;
        [Min(0)] public int weight;
    }

    private void OnEnable()
    {
        if (startOnEnable)
            StartWave();
    }

    private void OnDisable()
    {
        StopWave();
    }

    [ContextMenu("Start Wave")]
    public void StartWave()
    {
        if (_running != null) return;
        _running = StartCoroutine(SpawnWaveCoroutine());
    }

    [ContextMenu("Stop Wave")]
    public void StopWave()
    {
        if (_running == null) return;
        StopCoroutine(_running);
        _running = null;
    }

    private IEnumerator SpawnWaveCoroutine()
    {
        if (player == null)
        {
            Debug.LogError($"{nameof(WeightedOutOfRangeSpawner)}: Player가 비어있습니다.");
            _running = null;
            yield break;
        }

        if (spawnAreaHalfWidthX <= noSpawnHalfWidthX || spawnAreaHalfHeightY <= noSpawnHalfHeightY)
        {
            Debug.LogError($"{nameof(WeightedOutOfRangeSpawner)}: SpawnAreaHalf 값은 NoSpawnHalf 값보다 커야 합니다.");
            _running = null;
            yield break;
        }

        int spawned = 0;

        while (spawned < spawnCountPerWave)
        {
            int toSpawnThisBatch = Mathf.Min(spawnBatchSize, spawnCountPerWave - spawned);

            // 배치 내 겹침 방지(옵션)
            // 배치에서 이미 뽑은 위치들과 일정 거리 이상 떨어진 위치만 허용
            Vector3[] usedPositions = (minSeparationInBatch > 0f) ? new Vector3[toSpawnThisBatch] : null;
            int usedCount = 0;

            for (int i = 0; i < toSpawnThisBatch; i++)
            {
                var prefab = PickWeightedPrefab();
                if (prefab == null) continue;

                if (TryGetSpawnPosition(out Vector3 pos, usedPositions, usedCount))
                {
                    Instantiate(prefab, pos, Quaternion.identity, spawnParent);
                    spawned++;

                    if (usedPositions != null)
                        usedPositions[usedCount++] = pos;
                }
                else
                {
                    Debug.LogWarning($"{nameof(WeightedOutOfRangeSpawner)}: 유효한 스폰 위치를 찾지 못해 1회 스킵했습니다.");
                }
            }

            if (spawned >= spawnCountPerWave) break;

            if (spawnIntervalSeconds > 0f)
                yield return new WaitForSeconds(spawnIntervalSeconds);
            else
                yield return null;
        }

        _running = null;
    }

    private GameObject PickWeightedPrefab()
    {
        int wA = Mathf.Max(0, a.weight);
        int wB = Mathf.Max(0, b.weight);
        int wC = Mathf.Max(0, c.weight);

        int total = wA + wB + wC;
        if (total <= 0) return null;

        int r = UnityEngine.Random.Range(0, total);
        if (r < wA) return a.prefab;
        r -= wA;
        if (r < wB) return b.prefab;
        return c.prefab;
    }

    private bool TryGetSpawnPosition(out Vector3 pos, Vector3[] usedPositions, int usedCount)
    {
        Vector3 p = player.position;

        for (int t = 0; t < maxPositionTries; t++)
        {
            float dx = UnityEngine.Random.Range(-spawnAreaHalfWidthX, spawnAreaHalfWidthX);
            float dy = UnityEngine.Random.Range(-spawnAreaHalfHeightY, spawnAreaHalfHeightY);

            bool insideNoSpawn =
                Mathf.Abs(dx) <= noSpawnHalfWidthX &&
                Mathf.Abs(dy) <= noSpawnHalfHeightY;

            if (insideNoSpawn) continue;

            Vector3 candidate = new Vector3(p.x + dx, p.y + dy, spawnZ);

            if (minSeparationInBatch > 0f && usedPositions != null)
            {
                bool tooClose = false;
                for (int i = 0; i < usedCount; i++)
                {
                    if ((usedPositions[i] - candidate).sqrMagnitude < minSeparationInBatch * minSeparationInBatch)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;
            }

            pos = candidate;
            return true;
        }

        pos = default;
        return false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (player == null) return;

        Vector3 center = player.position;
        center.z = spawnZ;

        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.DrawWireCube(center, new Vector3(spawnAreaHalfWidthX * 2f, spawnAreaHalfHeightY * 2f, 0.01f));

        Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
        Gizmos.DrawWireCube(center, new Vector3(noSpawnHalfWidthX * 2f, noSpawnHalfHeightY * 2f, 0.01f));
    }
#endif
}
