using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EnemySpawnConfig
{
    public GameObject enemyPrefab;
    public float spawnInterval = 2f;
    public int amountPerSpawn = 1;

    [HideInInspector] public float timer;
}

public class EnemySpawner : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform baseTarget;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnRadius = 10f;

    [Header("Enemies")]
    [SerializeField] private List<EnemySpawnConfig> enemies;

    private void Update()
    {
        foreach (var enemy in enemies)
        {
            // ❗ Skip wenn Prefab fehlt
            if (enemy.enemyPrefab == null)
            {
                Debug.LogWarning("Enemy Prefab fehlt im Spawner!");
                continue;
            }

            enemy.timer += Time.deltaTime;

            if (enemy.timer >= enemy.spawnInterval)
            {
                enemy.timer = 0f;
                SpawnEnemyType(enemy);
            }
        }
    }

    private void SpawnEnemyType(EnemySpawnConfig config)
    {
        // Extra Safety
        if (config.enemyPrefab == null)
        {
            Debug.LogError("Spawn abgebrochen: Prefab ist NULL");
            return;
        }

        for (int i = 0; i < config.amountPerSpawn; i++)
        {
            // Zufälliger Winkel (360°)
            float angle = Random.Range(0f, Mathf.PI * 2);

            // 2D Kreis (XY Ebene)
            Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

            Vector3 spawnPos = baseTarget.position + dir * spawnRadius;

            // Instantiate
            GameObject enemy = Instantiate(config.enemyPrefab, spawnPos, Quaternion.identity);

            // Movement setzen
            EnemyMovement movement = enemy.GetComponent<EnemyMovement>();
            if (movement != null)
            {
                movement.baseTarget = baseTarget;
            }
            else
            {
                Debug.LogWarning("Enemy hat kein EnemyMovement Script!");
            }
        }
    }
}