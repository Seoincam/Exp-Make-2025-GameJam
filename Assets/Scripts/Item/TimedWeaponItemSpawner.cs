using System;
using System.Collections;
using System.Collections.Generic;
using Player.State;
using UnityEngine;

/// <summary>
/// Spawns one WeaponItem prefab around the player at a fixed interval.
/// Prefabs are loaded from Resources/Prefabs/Item.
/// </summary>
public class TimedWeaponItemSpawner : MonoBehaviour
{
    private const string ItemPrefabResourcesPath = "Prefabs/Item";
    private static readonly PlayerState[] WeaponTypes = (PlayerState[])Enum.GetValues(typeof(PlayerState));

    [Header("Target")]
    [SerializeField] private Transform player;
    [SerializeField] private string playerTag = "Player";

    [Header("Timing")]
    [Min(0.1f)]
    [SerializeField] private float spawnIntervalSeconds = 30f;
    [SerializeField] private bool spawnImmediatelyOnEnable = false;

    [Header("Spawn Area (Around Player)")]
    [Min(0f)]
    [SerializeField] private float minSpawnDistance = 2f;
    [Min(0.1f)]
    [SerializeField] private float maxSpawnDistance = 6f;
    [SerializeField] private float spawnZ = 0f;

    [Header("Debug")]
    [SerializeField] private bool logWarnings = true;

    private readonly List<GameObject> _weaponItemPrefabs = new();
    private Coroutine _running;

    private void Awake()
    {
        CacheWeaponItemPrefabs();
        TryResolvePlayer();
    }

    private void OnEnable()
    {
        StartSpawning();
    }

    private void OnDisable()
    {
        StopSpawning();
    }

    private void OnValidate()
    {
        if (maxSpawnDistance < minSpawnDistance)
        {
            maxSpawnDistance = minSpawnDistance;
        }

        spawnIntervalSeconds = Mathf.Max(0.1f, spawnIntervalSeconds);
    }

    [ContextMenu("Start Spawning")]
    public void StartSpawning()
    {
        if (_running != null)
        {
            return;
        }

        _running = StartCoroutine(SpawnLoopCoroutine());
    }

    [ContextMenu("Stop Spawning")]
    public void StopSpawning()
    {
        if (_running == null)
        {
            return;
        }

        StopCoroutine(_running);
        _running = null;
    }

    [ContextMenu("Reload Item Prefabs")]
    public void ReloadItemPrefabs()
    {
        CacheWeaponItemPrefabs();
    }

    [ContextMenu("Spawn One Item Now")]
    public void SpawnOneItemNow()
    {
        SpawnSingleItem();
    }

    private IEnumerator SpawnLoopCoroutine()
    {
        if (spawnImmediatelyOnEnable)
        {
            SpawnSingleItem();
        }

        while (true)
        {
            yield return new WaitForSeconds(spawnIntervalSeconds);
            SpawnSingleItem();
        }
    }

    private void SpawnSingleItem()
    {
        if (!TryResolvePlayer())
        {
            if (logWarnings)
            {
                Debug.LogWarning($"{nameof(TimedWeaponItemSpawner)}: player not found.");
            }
            return;
        }

        if (_weaponItemPrefabs.Count == 0)
        {
            CacheWeaponItemPrefabs();
        }

        if (_weaponItemPrefabs.Count == 0)
        {
            if (logWarnings)
            {
                Debug.LogWarning($"{nameof(TimedWeaponItemSpawner)}: no WeaponItem prefabs under Resources/{ItemPrefabResourcesPath}.");
            }
            return;
        }

        int randomIndex = UnityEngine.Random.Range(0, _weaponItemPrefabs.Count);
        GameObject prefab = _weaponItemPrefabs[randomIndex];
        if (!prefab)
        {
            return;
        }

        Vector3 spawnPos = GetRandomPositionAroundPlayer();
        GameObject spawned = Instantiate(prefab, spawnPos, Quaternion.identity);
        InitializeWeaponItemFromRandomType(spawned);
    }

    private Vector3 GetRandomPositionAroundPlayer()
    {
        float minR = Mathf.Max(0f, minSpawnDistance);
        float maxR = Mathf.Max(minR, maxSpawnDistance);
        float radius = UnityEngine.Random.Range(minR, maxR);
        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

        Vector3 center = player.position;
        float x = center.x + Mathf.Cos(angle) * radius;
        float y = center.y + Mathf.Sin(angle) * radius;
        return new Vector3(x, y, spawnZ);
    }

    private bool TryResolvePlayer()
    {
        if (player)
        {
            return true;
        }

        if (string.IsNullOrEmpty(playerTag))
        {
            return false;
        }

        GameObject found = GameObject.FindGameObjectWithTag(playerTag);
        if (!found)
        {
            return false;
        }

        player = found.transform;
        return true;
    }

    private void CacheWeaponItemPrefabs()
    {
        _weaponItemPrefabs.Clear();

        GameObject[] loaded = Resources.LoadAll<GameObject>(ItemPrefabResourcesPath);
        for (int i = 0; i < loaded.Length; i++)
        {
            GameObject prefab = loaded[i];
            if (!prefab)
            {
                continue;
            }

            if (!HasWeaponItemComponent(prefab))
            {
                continue;
            }

            _weaponItemPrefabs.Add(prefab);
        }
    }

    private static bool HasWeaponItemComponent(GameObject prefab)
    {
        if (prefab.GetComponent<WeaponItem>())
        {
            return true;
        }

        return prefab.GetComponentInChildren<WeaponItem>(true) != null;
    }

    private void InitializeWeaponItemFromRandomType(GameObject spawned)
    {
        if (!spawned)
        {
            return;
        }

        var weaponItem = spawned.GetComponent<WeaponItem>();
        if (!weaponItem)
        {
            weaponItem = spawned.GetComponentInChildren<WeaponItem>(true);
        }

        if (!weaponItem)
        {
            if (logWarnings)
            {
                Debug.LogWarning($"{nameof(TimedWeaponItemSpawner)}: spawned object has no {nameof(WeaponItem)} component.");
            }
            return;
        }

        PlayerState randomType = PickRandomWeaponType();
        weaponItem.Initialize(randomType);
    }

    private static PlayerState PickRandomWeaponType()
    {
        if (WeaponTypes == null || WeaponTypes.Length == 0)
        {
            return PlayerState.Anchovy;
        }

        int randomIndex = UnityEngine.Random.Range(0, WeaponTypes.Length);
        return WeaponTypes[randomIndex];
    }
}
