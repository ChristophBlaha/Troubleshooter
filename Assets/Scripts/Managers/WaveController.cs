using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Verwaltet den Übergang zwischen Wellen
/// Tracking: Alle feinde der aktuellen Welle zerstört?
/// </summary>
public class WaveController : MonoBehaviour
{
    public static WaveController Instance { get; private set; }

    private WaveManager waveManager;
    private int currentWaveEnemyCount = 0;
    private int currentWaveEnemiesKilled = 0;
    private bool isWaveActive = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        waveManager = WaveManager.Instance;
        if (waveManager != null)
        {
            waveManager.OnWaveStart.AddListener(OnWaveStarted);
        }
    }

    private void OnWaveStarted(int waveNumber)
    {
        isWaveActive = true;
        currentWaveEnemyCount = waveManager.GetCurrentWaveConfig().totalSpawnCount;
        currentWaveEnemiesKilled = 0;

        Debug.Log($"[WaveController] Wave {waveNumber} started. Units to resolve: {currentWaveEnemyCount}");
    }

    public void RegisterEnemyDeath()
    {
        if (!isWaveActive) return;

        currentWaveEnemiesKilled++;
        Debug.Log($"[WaveController] Enemy killed: {currentWaveEnemiesKilled}/{currentWaveEnemyCount}");

        if (currentWaveEnemiesKilled >= currentWaveEnemyCount)
        {
            CompleteWave();
        }
    }

    private void CompleteWave()
    {
        isWaveActive = false;
        Debug.Log("[WaveController] Wave completed! Starting next wave...");
        
        if (waveManager != null)
        {
            waveManager.CompleteWave();
            waveManager.StartNextWave();
        }
    }
}
