using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Main Menu UI Controller
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button highscoresButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject highscoresPanel;
    [SerializeField] private Button backButton;

    private void Start()
    {
        // Ensure time is running
        Time.timeScale = 1f;

        // Button events
        if (playButton != null)
            playButton.onClick.AddListener(PlayGame);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);

        if (highscoresButton != null)
            highscoresButton.onClick.AddListener(OpenHighScores);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        if (backButton != null)
            backButton.onClick.AddListener(ClosePanel);

        // Hide panels
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        if (highscoresPanel != null)
            highscoresPanel.SetActive(false);
        else
        {
            // Try to find a highscores panel instance in the scene if not assigned in inspector
            var found = GameObject.Find("HighscoresPanel");
            if (found != null)
            {
                highscoresPanel = found;
                highscoresPanel.SetActive(false);
                Debug.Log("[MainMenuUI] Auto-found highscoresPanel in scene.");
            }
        }
    }

    public void PlayGame()
    {
        Debug.Log("Starting game...");
        SceneManager.LoadScene("SampleScene");
    }

    public void OpenSettings()
    {
        Debug.Log("Opening settings...");
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void OpenHighScores()
    {
        Debug.Log("Opening high scores...");
        if (highscoresPanel != null)
        {
            highscoresPanel.SetActive(true);
            // Prefer a dedicated panel controller if present
            var panelUI = highscoresPanel.GetComponent<HighscoresPanelUI>();
            if (panelUI != null)
            {
                panelUI.UpdateFromManager();
            }
            else
            {
                UpdateHighScoresDisplay();
            }
        }
    }

    private void UpdateHighScoresDisplay()
    {
        if (HighScoreManager.Instance == null) return;

        var scores = HighScoreManager.Instance.GetHighScores();
        TextMeshProUGUI scoresText = highscoresPanel?.GetComponentInChildren<TextMeshProUGUI>();

        if (scoresText != null)
        {
            string text = "=== HIGH SCORES ===\n\n";
            for (int i = 0; i < scores.Count; i++)
            {
                text += $"{i+1}. {scores[i].playerName} - {scores[i].score} (Wave {scores[i].wave})\n";
            }
            if (scores.Count == 0)
                text += "No scores yet!";

            scoresText.text = text;
        }
    }

    public void ClosePanel()
    {
        if (settingsPanel != null && settingsPanel.activeInHierarchy)
            settingsPanel.SetActive(false);
        if (highscoresPanel != null && highscoresPanel.activeInHierarchy)
            highscoresPanel.SetActive(false);
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
