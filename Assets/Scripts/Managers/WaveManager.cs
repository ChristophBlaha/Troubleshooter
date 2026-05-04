using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Verwaltet Wave-Runden und Schwierigkeitsprogression
/// </summary>
public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [System.Serializable]
    public class WaveConfig
    {
        public int waveNumber;
        public int enemyCount;
        public float spawnInterval;
        public float enemyHealthMultiplier;
        public float enemySpeedMultiplier;
    }

    [SerializeField] private float baseEnemyCount = 3f;
    [SerializeField] private float baseSpawnInterval = 2f;
    [SerializeField] private float baseDifficultyMultiplier = 1.3f;
    [SerializeField] private float wavePauseDuration = 3f;

    private int currentWave = 0;
    private WaveConfig currentWaveConfig;
    private bool isWaveActive = false;
    private float waveTimer = 0f;
    private bool isWavePaused = true;

    public UnityEvent<int> OnWaveStart = new UnityEvent<int>();
    public UnityEvent<int> OnWaveComplete = new UnityEvent<int>();
    public UnityEvent OnAllWavesComplete = new UnityEvent();

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
        StartNextWave();
    }

    private void Update()
    {
        if (isWavePaused)
        {
            waveTimer -= Time.deltaTime;
            if (waveTimer <= 0f)
            {
                isWavePaused = false;
                OnWaveStart?.Invoke(currentWave);
            }
        }
    }

    public void StartNextWave()
    {
        currentWave++;
        currentWaveConfig = GenerateWaveConfig(currentWave);
        
        // Welle starten nach Pause
        waveTimer = wavePauseDuration;
        isWavePaused = true;
        isWaveActive = true;

        Debug.Log($"[WaveManager] Wave {currentWave} startet in {wavePauseDuration}s");
    }

    public void CompleteWave()
    {
        isWaveActive = false;
        OnWaveComplete?.Invoke(currentWave);
        Debug.Log($"[WaveManager] Wave {currentWave} abgeschlossen!");
        
        // Nächste Welle wird manuell durch Spawner ausgelöst
    }

    private WaveConfig GenerateWaveConfig(int waveNumber)
    {
        // Schwierigkeitsmultiplikator: 1.0 → 1.3 → 1.69 → ...
        float difficultyMult = Mathf.Pow(baseDifficultyMultiplier, waveNumber - 1);

        return new WaveConfig
        {
            waveNumber = waveNumber,
            enemyCount = Mathf.Max(1, Mathf.RoundToInt(baseEnemyCount * difficultyMult)),
            spawnInterval = Mathf.Max(0.5f, baseSpawnInterval / difficultyMult),
            enemyHealthMultiplier = 1f + (waveNumber - 1) * 0.15f,
            enemySpeedMultiplier = 1f + (waveNumber - 1) * 0.1f
        };
    }

    public WaveConfig GetCurrentWaveConfig() => currentWaveConfig;
    public int GetCurrentWave() => currentWave;
    public bool IsWaveActive() => isWaveActive;
    public bool IsWavePaused() => isWavePaused;

    // Debug helper to inspect internal wave state
    public void DebugLogState()
    {
        Debug.Log($"[WaveManager] State => currentWave={currentWave}, isWaveActive={isWaveActive}, isWavePaused={isWavePaused}, waveTimer={waveTimer}, timeScale={Time.timeScale}");
    }
}
