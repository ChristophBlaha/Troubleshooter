using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Verwaltet Highscores mit Persistierung
/// </summary>
public class HighScoreManager : MonoBehaviour
{
    public static HighScoreManager Instance { get; private set; }

    [System.Serializable]
    public class HighScore
    {
        public string playerName;
        public int score;
        public int wave;
        public string date;

        public HighScore(string name, int pts, int w)
        {
            playerName = name;
            score = pts;
            wave = w;
            date = System.DateTime.Now.ToString("dd.MM.yyyy HH:mm");
        }
    }

    [SerializeField] private int maxHighScores = 10;
    private List<HighScore> highScores = new List<HighScore>();
    private const string SAVE_KEY = "HighScores_Troubleshooter";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        LoadHighScores();
    }

    public void SaveScore(string playerName, int score, int wave)
    {
        HighScore newScore = new HighScore(playerName, score, wave);
        highScores.Add(newScore);

        // Sortieren (höchste Scores zuerst) und begrenzen
        highScores = highScores.OrderByDescending(s => s.score).Take(maxHighScores).ToList();

        PersistHighScores();
        Debug.Log($"[HighScoreManager] Score gespeichert: {playerName} - {score} (Wave {wave})");
    }

    public List<HighScore> GetHighScores() => new List<HighScore>(highScores);

    public bool IsHighScore(int score)
    {
        if (highScores.Count < maxHighScores) return true;
        return score > highScores.Last().score;
    }

    private void PersistHighScores()
    {
        // JSON Serialisierung mit List wrapper
        HighScoreList wrapper = new HighScoreList { scores = highScores };
        string json = JsonUtility.ToJson(wrapper, true);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    private void LoadHighScores()
    {
        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            string json = PlayerPrefs.GetString(SAVE_KEY);
            HighScoreList wrapper = JsonUtility.FromJson<HighScoreList>(json);
            highScores = wrapper.scores ?? new List<HighScore>();
        }
        else
        {
            highScores = new List<HighScore>();
        }
    }

    public void ClearAllScores()
    {
        highScores.Clear();
        PlayerPrefs.DeleteKey(SAVE_KEY);
        Debug.Log("[HighScoreManager] Alle Highscores gelöscht");
    }

    // Helper Klasse für JSON Serialisierung
    [System.Serializable]
    private class HighScoreList
    {
        public List<HighScore> scores = new List<HighScore>();
    }
}
