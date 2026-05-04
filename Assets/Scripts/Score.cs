using UnityEngine;
using System;
using TMPro;

public class Score : MonoBehaviour
{
    private int score;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI waveText;
    public static Score Instance { get; private set; }
    
    private WaveManager waveManager;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        waveManager = WaveManager.Instance;
        if (waveManager != null)
        {
            waveManager.OnWaveStart.AddListener(UpdateWaveDisplay);
        }
        UpdateWaveDisplay(1);
    }

    private void Update()
    {
        if(scoreText!=null)
            scoreText.text = $"Score: {score}";
    }

    public void IncreaseScore(int amount)
    {
        score += amount;
        if (AudioManager.Instance)
        {
            AudioManager.Instance.PlaySFX("score_gained", 0.6f);
        }
        Debug.Log($"Score increased: {amount}, Total: {score}");
    }

    private void UpdateWaveDisplay(int waveNumber)
    {
        if (waveText != null)
        {
            waveText.text = $"Wave: {waveNumber}";
        }
    }
    
    public int GetScore()
    {
        return score;
    }

    public int GetCurrentWave()
    {
        return waveManager ? waveManager.GetCurrentWave() : 1;
    }
}
