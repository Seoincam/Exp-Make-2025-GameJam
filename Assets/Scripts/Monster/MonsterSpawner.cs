using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns monsters outside a player-centered no-spawn box.
/// - Wave: spawns spawnCountPerWave in total
/// - Batch: spawns spawnBatchSize each burst, then waits spawnIntervalSeconds
/// - Prefab selection: weight is computed from round and each monster's tier (tier 1 = early rounds, higher tier = later rounds).
/// </summary>
public class WeightedOutOfRangeSpawner : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Wave Settings")]
    [Min(1)]
    [SerializeField] private int spawnCountPerWave = 6;

    [Tooltip("If true, starts a new wave again after finishing one wave.")]
    [SerializeField] private bool repeatWaves = true;

    [Tooltip("Interval between batches (seconds)")]
    [Min(0f)]
    [SerializeField] private float spawnIntervalSeconds = 1f;

    [Tooltip("Interval between waves (seconds)")]
    [Min(0f)]
    [SerializeField] private float waveIntervalSeconds = 0f;

    [Tooltip("How many monsters are spawned per batch")]
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

    [Tooltip("Minimum separation in same batch (0 = no separation check)")]
    [Min(0f)]
    [SerializeField] private float minSeparationInBatch = 0.5f;

    [SerializeField] private Transform spawnParent;

    [Header("Monster Prefabs (Ratio by Round)")]
    [Tooltip("Each monster has a tier (1 = early rounds, 2 = mid, 3 = late...). Spawn ratio is computed from current round vs tier.")]
    [SerializeField] private List<MonsterPrefabEntry> monsterPrefabs = new();
    [Tooltip("Peak weight when monster tier equals current round.")]
    [Min(1)]
    [SerializeField] private int roundPeakWeight = 10;
    [Tooltip("Weight decrease per tier difference from round (e.g. 2 = tier 2 at round 1 gets weight 8).")]
    [Min(0f)]
    [SerializeField] private float tierFalloff = 2f;
    [Tooltip("Extra weight added when entry is marked as basic mob. Keeps them appearing often even in later rounds.")]
    [Min(0)]
    [SerializeField] private int basicMobWeightBonus = 5;

    [Header("Auto Start")]
    [SerializeField] private bool startOnEnable = true;

    private Coroutine _running;

    [Serializable]
    private struct MonsterPrefabEntry
    {
        public GameObject prefab;
        [Tooltip("1 = early rounds, 2 = mid, 3 = late... Closer to current round = higher spawn chance.")]
        [Min(1)]
        public int tier;
        [Tooltip("If true, adds basicMobWeightBonus to weight so this monster appears often even when round doesn't match tier.")]
        public bool isBasicMob;
    }

    [Serializable]
    private struct WeightedPrefab
    {
        public GameObject prefab;
        [Min(0)] public int weight;
    }

    private void OnEnable()
    {
        if (startOnEnable)
        {
            StartWave();
        }
    }

    private void OnDisable()
    {
        StopWave();
    }

    [ContextMenu("Start Wave")]
    public void StartWave()
    {
        if (_running != null)
        {
            return;
        }

        _running = StartCoroutine(SpawnWaveCoroutine());
    }

    [ContextMenu("Stop Wave")]
    public void StopWave()
    {
        if (_running == null)
        {
            return;
        }

        StopCoroutine(_running);
        _running = null;
    }

    private IEnumerator SpawnWaveCoroutine()
    {
        int currentStageDifficulty = 1;
        if (GameManager.Instance != null)
        {
            currentStageDifficulty = Mathf.Max(1, GameManager.Instance.CurrentStage);
        }

        if (player == null)
        {
            Debug.LogError($"{nameof(WeightedOutOfRangeSpawner)}: player is null.");
            _running = null;
            yield break;
        }

        if (spawnAreaHalfWidthX <= noSpawnHalfWidthX || spawnAreaHalfHeightY <= noSpawnHalfHeightY)
        {
            Debug.LogError($"{nameof(WeightedOutOfRangeSpawner)}: SpawnAreaHalf values must be larger than NoSpawnHalf values.");
            _running = null;
            yield break;
        }

        while (true)
        {
            int spawned = 0;

            while (spawned < spawnCountPerWave)
            {
                int toSpawnThisBatch = Mathf.Min(spawnBatchSize, spawnCountPerWave - spawned);

                Vector3[] usedPositions = (minSeparationInBatch > 0f) ? new Vector3[toSpawnThisBatch] : null;
                int usedCount = 0;

                for (int i = 0; i < toSpawnThisBatch; i++)
                {
                    var prefab = PickWeightedPrefab();
                    if (prefab == null)
                    {
                        continue;
                    }

                    if (TryGetSpawnPosition(out Vector3 pos, usedPositions, usedCount))
                    {
                        Instantiate(prefab, pos, Quaternion.identity, spawnParent);
                        spawned++;

                        if (usedPositions != null)
                        {
                            usedPositions[usedCount++] = pos;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"{nameof(WeightedOutOfRangeSpawner)}: failed to find valid spawn position.");
                    }
                }

                if (spawned >= spawnCountPerWave)
                {
                     spawnCountPerWave =currentStageDifficulty*(currentStageDifficulty+3);
                     if(spawnCountPerWave <12){
                        spawnCountPerWave +=3;
                        spawnBatchSize = spawnCountPerWave/3;
                     }
                     else if(spawnCountPerWave <40){
                        spawnCountPerWave +=10;
                        spawnBatchSize = spawnCountPerWave/10;
                     }
                     else{
                        spawnCountPerWave = 50;
                        spawnBatchSize = spawnCountPerWave/20;
                     }
                     Debug.Log("난이도 증가!");
                    break;
                }

                if (spawnIntervalSeconds > 0f)
                {
                    yield return new WaitForSeconds(spawnIntervalSeconds);
                }
                else
                {
                    yield return null;
                }
            }

            if (!repeatWaves)
            {
                break;
            }

            if (waveIntervalSeconds > 0f)
            {
                yield return new WaitForSeconds(waveIntervalSeconds);
            }
            else
            {
                yield return null;
            }
        }
        
       



        _running = null;
    }

    private GameObject PickWeightedPrefab()
    {
        var candidates = GetComputedWeightsForCurrentRound();
        if (candidates == null || candidates.Count == 0)
        {
            return null;
        }

        int total = 0;
        for (int i = 0; i < candidates.Count; i++)
        {
            if (!candidates[i].prefab)
            {
                continue;
            }

            total += Mathf.Max(0, candidates[i].weight);
        }

        if (total <= 0)
        {
            return null;
        }

        int r = UnityEngine.Random.Range(0, total);

        for (int i = 0; i < candidates.Count; i++)
        {
            var candidate = candidates[i];
            if (!candidate.prefab)
            {
                continue;
            }

            int weight = Mathf.Max(0, candidate.weight);
            if (weight <= 0)
            {
                continue;
            }

            if (r < weight)
            {
                return candidate.prefab;
            }

            r -= weight;
        }

        return null;
    }

    /// <summary>
    /// Computes spawn weights for each monster from current round and tier.
    /// Weight = max(0, roundPeakWeight - tierFalloff * |tier - round|).
    /// </summary>
    private List<WeightedPrefab> GetComputedWeightsForCurrentRound()
    {
        int round = 1;
        if (GameManager.Instance != null)
        {
            round = Mathf.Max(1, GameManager.Instance.CurrentStage);
        }

        var result = new List<WeightedPrefab>();
        for (int i = 0; i < monsterPrefabs.Count; i++)
        {
            var entry = monsterPrefabs[i];
            if (entry.prefab == null)
            {
                continue;
            }

            int tierDiff = Mathf.Abs(entry.tier - round);
            float w = roundPeakWeight - tierFalloff * tierDiff;
            int weight = Mathf.Max(0, Mathf.RoundToInt(w));
            if (entry.isBasicMob && weight <= basicMobWeightBonus)
            {
                weight += basicMobWeightBonus;
            }
            if (weight <= 0)
            {
                continue;
            }

            result.Add(new WeightedPrefab { prefab = entry.prefab, weight = weight });
        }

        return result;
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

            if (insideNoSpawn)
            {
                continue;
            }

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

                if (tooClose)
                {
                    continue;
                }
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
        if (player == null)
        {
            return;
        }

        Vector3 center = player.position;
        center.z = spawnZ;

        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.DrawWireCube(center, new Vector3(spawnAreaHalfWidthX * 2f, spawnAreaHalfHeightY * 2f, 0.01f));

        Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
        Gizmos.DrawWireCube(center, new Vector3(noSpawnHalfWidthX * 2f, noSpawnHalfHeightY * 2f, 0.01f));
    }
#endif
}
