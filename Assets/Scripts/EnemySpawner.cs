using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EnemySpawnConfig
{
    public GameObject enemyPrefab;
}

public class EnemySpawner : MonoBehaviour
{
    private enum SpawnRole
    {
        Hostile,
        Friendly
    }

    [Header("Target")]
    [SerializeField] private Transform baseTarget;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnRadius = 10f;

    [Header("Enemies")]
    [SerializeField] private List<EnemySpawnConfig> enemies;

    private WaveManager waveManager;
    private int spawnedUnitsThisWave = 0;
    private WaveManager.WaveConfig currentWaveConfig;
    private float spawnTimer = 0f;
    private readonly List<SpawnRole> spawnSchedule = new List<SpawnRole>();
    private readonly List<EnemySpawnConfig> hostilePrefabs = new List<EnemySpawnConfig>();
    private readonly List<EnemySpawnConfig> friendlyPrefabs = new List<EnemySpawnConfig>();

    private void Start()
    {
        CategorizePrefabs();
        waveManager = WaveManager.Instance;
        if (waveManager != null)
        {
            waveManager.OnWaveStart.AddListener(OnWaveStart);
        }
    }

    private void OnWaveStart(int waveNumber)
    {
        CategorizePrefabs();
        currentWaveConfig = waveManager.GetCurrentWaveConfig();
        spawnedUnitsThisWave = 0;
        spawnTimer = 0f;
        BuildSpawnSchedule(currentWaveConfig.hostileCount, currentWaveConfig.friendlyCount);
        Debug.Log($"[EnemySpawner] Wave {waveNumber} gestartet. Hostiles: {currentWaveConfig.hostileCount}, Friendlies: {currentWaveConfig.friendlyCount}");
    }

    private void Update()
    {
        if (!waveManager || !waveManager.IsWaveActive() || waveManager.IsWavePaused())
            return;

        if (spawnedUnitsThisWave >= spawnSchedule.Count)
            return;

        spawnTimer += Time.deltaTime;

        if (spawnTimer < currentWaveConfig.spawnInterval)
            return;

        spawnTimer = 0f;

        SpawnNextUnit(spawnSchedule[spawnedUnitsThisWave]);
        spawnedUnitsThisWave++;

        if (spawnedUnitsThisWave >= spawnSchedule.Count)
        {
            Debug.Log("[EnemySpawner] Alle Einheiten dieser Wave gespawnt!");
        }
    }

    private void SpawnNextUnit(SpawnRole spawnRole)
    {
        if (enemies == null || enemies.Count == 0)
        {
            Debug.LogWarning("EnemySpawner: Keine Enemy-Prefabs in der Liste!");
            return;
        }

        List<EnemySpawnConfig> validEnemies = spawnRole == SpawnRole.Friendly ? friendlyPrefabs : hostilePrefabs;
        if (validEnemies.Count == 0)
        {
            Debug.LogWarning($"EnemySpawner: Keine Prefabs fuer Rolle {spawnRole} gefunden!");
            return;
        }

        EnemySpawnConfig config = GetRandomValidConfig(validEnemies);
        if (config == null || config.enemyPrefab == null)
        {
            Debug.LogWarning($"EnemySpawner: Spawn fuer Rolle {spawnRole} uebersprungen, weil kein gueltiges Prefab verfuegbar ist.");
            if (WaveController.Instance != null)
                WaveController.Instance.RegisterEnemyDeath();
            return;
        }

        // Extra Safety
        // Zufaelliger Winkel (360°)
        float angle = Random.Range(0f, Mathf.PI * 2);

        // 2D Kreis (XY Ebene)
        Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

        Vector3 spawnPos = baseTarget.position + dir * spawnRadius;

        // Instantiate
        GameObject enemy = Instantiate(config.enemyPrefab, spawnPos, Quaternion.identity);

        // Movement setzen + Speed anpassen
        EnemyMovement movement = enemy.GetComponent<EnemyMovement>();
        if (movement != null)
        {
            movement.baseTarget = baseTarget;
            if (spawnRole == SpawnRole.Hostile)
                movement.Speed *= currentWaveConfig.enemySpeedMultiplier;
            else
                movement.Speed *= 1.45f;
        }
        else
        {
            Debug.LogWarning("Enemy hat kein EnemyMovement Script!");
        }

        // Health anpassen
        GazeDamageable damageable = enemy.GetComponent<GazeDamageable>();
        if (damageable != null)
        {
            if (spawnRole == SpawnRole.Hostile)
            {
                damageable.MaxHealth = Mathf.RoundToInt(damageable.MaxHealth * currentWaveConfig.enemyHealthMultiplier);
                damageable.ConfigureRuntime(DamageableTeam.Hostile, damageable.MaxHealth, true, false);
            }
            else
            {
                damageable.ConfigureRuntime(DamageableTeam.Friendly, damageable.MaxHealth, true, false);
            }

            damageable.currentHealth = damageable.MaxHealth;
        }

        // Audio beim Spawn
        if (AudioManager.Instance)
        {
            AudioManager.Instance.PlaySFX("enemy_spawn", 0.6f);
        }
    }

    private void CategorizePrefabs()
    {
        hostilePrefabs.Clear();
        friendlyPrefabs.Clear();

        if (enemies == null)
            return;

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemySpawnConfig config = enemies[i];
            if (!IsValidSpawnPrefab(config))
                continue;

            if (config.enemyPrefab.GetComponent<FriendReturningHome>() != null)
                friendlyPrefabs.Add(config);
            else
                hostilePrefabs.Add(config);
        }
    }

    private void BuildSpawnSchedule(int hostileCount, int friendlyCount)
    {
        spawnSchedule.Clear();

        if (hostileCount <= 0)
            return;

        if (friendlyCount <= 0)
        {
            for (int i = 0; i < hostileCount; i++)
                spawnSchedule.Add(SpawnRole.Hostile);

            return;
        }

        // Front-load friendlies a bit more so they reach the base early enough
        // to contribute meaningfully before the wave is almost over.
        int earlyHostileBudget = Mathf.Clamp(Mathf.CeilToInt(hostileCount * 0.4f), friendlyCount + 1, hostileCount);
        int earlySegments = friendlyCount + 1;
        int earlyHostilePerSegment = earlyHostileBudget / earlySegments;
        int earlyHostileRemainder = earlyHostileBudget % earlySegments;

        for (int segment = 0; segment < earlySegments; segment++)
        {
            int hostilesThisSegment = earlyHostilePerSegment + (segment < earlyHostileRemainder ? 1 : 0);
            for (int i = 0; i < hostilesThisSegment; i++)
            {
                spawnSchedule.Add(SpawnRole.Hostile);
            }

            if (segment < friendlyCount)
                spawnSchedule.Add(SpawnRole.Friendly);
        }

        int remainingHostiles = hostileCount - earlyHostileBudget;
        for (int i = 0; i < remainingHostiles; i++)
        {
            spawnSchedule.Add(SpawnRole.Hostile);
        }
    }

    private EnemySpawnConfig GetRandomValidConfig(List<EnemySpawnConfig> configs)
    {
        List<EnemySpawnConfig> validConfigs = new List<EnemySpawnConfig>();
        for (int i = 0; i < configs.Count; i++)
        {
            if (IsValidSpawnPrefab(configs[i]))
                validConfigs.Add(configs[i]);
        }

        if (validConfigs.Count == 0)
            return null;

        return validConfigs[Random.Range(0, validConfigs.Count)];
    }

    private bool IsValidSpawnPrefab(EnemySpawnConfig config)
    {
        if (config == null || config.enemyPrefab == null)
            return false;

        GameObject prefab = config.enemyPrefab;
        if (prefab.scene.IsValid())
            return false;

        return true;
    }
}
