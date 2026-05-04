using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EnemySpawnConfig
{
    public GameObject enemyPrefab;

}

public class EnemySpawner : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform baseTarget;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnRadius = 10f;

    [Header("Enemies")]
    [SerializeField] private List<EnemySpawnConfig> enemies;

    private WaveManager waveManager;
    private int enemiesSpawnedThisWave = 0;
    private WaveManager.WaveConfig currentWaveConfig;
    private float spawnTimer = 0f;

    private void Start()
    {
        waveManager = WaveManager.Instance;
        if (waveManager != null)
        {
            waveManager.OnWaveStart.AddListener(OnWaveStart);
        }
    }

    private void OnWaveStart(int waveNumber)
    {
        currentWaveConfig = waveManager.GetCurrentWaveConfig();
        enemiesSpawnedThisWave = 0;
        spawnTimer = 0f;
        Debug.Log($"[EnemySpawner] Wave {waveNumber} gestartet. Sollen spawnen: {currentWaveConfig.enemyCount} Feinde");
    }

    private void Update()
    {
        if (!waveManager || !waveManager.IsWaveActive() || waveManager.IsWavePaused())
            return;

        if (enemiesSpawnedThisWave >= currentWaveConfig.enemyCount)
            return;

        spawnTimer += Time.deltaTime;

        if (spawnTimer < currentWaveConfig.spawnInterval)
            return;

        spawnTimer = 0f;

        SpawnNextEnemy();
        enemiesSpawnedThisWave++;

        if (enemiesSpawnedThisWave >= currentWaveConfig.enemyCount)
        {
            Debug.Log($"[EnemySpawner] Alle Feinde dieser Wave gespawnt!");
        }
    }

    private void SpawnNextEnemy()
    {
        if (enemies == null || enemies.Count == 0)
        {
            Debug.LogWarning("EnemySpawner: Keine Enemy-Prefabs in der Liste!");
            return;
        }

        List<EnemySpawnConfig> validEnemies = enemies.FindAll(e => e != null && e.enemyPrefab != null);
        if (validEnemies.Count == 0)
        {
            Debug.LogWarning("EnemySpawner: Alle Enemy-Prefabs sind NULL!");
            return;
        }

        EnemySpawnConfig config = validEnemies[Random.Range(0, validEnemies.Count)];

        // Extra Safety
        if (config.enemyPrefab == null)
        {
            Debug.LogError("Spawn abgebrochen: Prefab ist NULL");
            return;
        }

        // Zufälliger Winkel (360°)
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
            movement.Speed *= currentWaveConfig.enemySpeedMultiplier;
        }
        else
        {
            Debug.LogWarning("Enemy hat kein EnemyMovement Script!");
        }

        // Health anpassen
        GazeDamageable damageable = enemy.GetComponent<GazeDamageable>();
        if (damageable != null)
        {
            damageable.MaxHealth = Mathf.RoundToInt(damageable.MaxHealth * currentWaveConfig.enemyHealthMultiplier);
            damageable.currentHealth = damageable.MaxHealth;
        }

        // Audio beim Spawn
        if (AudioManager.Instance)
        {
            AudioManager.Instance.PlaySFX("enemy_spawn", 0.6f);
        }
    }
}