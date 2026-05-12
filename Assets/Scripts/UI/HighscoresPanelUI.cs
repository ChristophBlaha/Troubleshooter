using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class HighscoresPanelUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoresText;

    public void ShowScores(List<HighScoreManager.HighScore> scores)
    {
        if (scoresText == null)
        {
            Debug.LogWarning("[HighscoresPanelUI] scoresText is null");
            return;
        }

        Debug.Log($"[HighscoresPanelUI] ShowScores called with {scores?.Count ?? 0} entries");

        string text = "COMMAND SCOREBOARD\n\n";
        if (scores != null && scores.Count > 0)
        {
            for (int i = 0; i < scores.Count; i++)
            {
                text += $"{i + 1}. {scores[i].playerName}  //  {scores[i].score}  //  WAVE {scores[i].wave}\n";
            }
        }
        else
        {
            text += "NO COMBAT RECORDS YET";
        }

        scoresText.text = text;
        Debug.Log("[HighscoresPanelUI] Scores text updated");
    }

    public void UpdateFromManager()
    {
        if (HighScoreManager.Instance != null)
        {
            var s = HighScoreManager.Instance.GetHighScores();
            Debug.Log($"[HighscoresPanelUI] Updating from HighScoreManager.Instance ({s.Count} entries)");
            ShowScores(s);
            return;
        }

        // Fallback: try to load from PlayerPrefs directly
        const string SAVE_KEY = "HighScores_Troubleshooter";
        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            string json = PlayerPrefs.GetString(SAVE_KEY);
            try
            {
                var wrapper = JsonUtility.FromJson<HighScoreListWrapper>(json);
                var list = wrapper?.scores ?? new List<HighScoreManager.HighScore>();
                Debug.Log($"[HighscoresPanelUI] Loaded {list.Count} entries from PlayerPrefs fallback");
                ShowScores(list);
                return;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[HighscoresPanelUI] Failed to parse PlayerPrefs highscore JSON: {ex.Message}");
            }
        }

        Debug.Log("[HighscoresPanelUI] No HighScoreManager and no saved PlayerPrefs entries");
    }

    [System.Serializable]
    private class HighScoreListWrapper
    {
        public List<HighScoreManager.HighScore> scores = new List<HighScoreManager.HighScore>();
    }
}
