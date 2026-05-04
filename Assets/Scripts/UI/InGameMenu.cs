using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Toggle a pause/menu overlay during gameplay with Settings, Highscores and Quit.
/// Press Escape to open/close. Pauses the game when open.
/// </summary>
public class InGameMenu : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel; // Root panel that contains buttons
    [SerializeField] private GameObject settingsPanel; // optional settings panel (reuse existing)
    [SerializeField] private GameObject highscoresPanel; // optional highscores panel (reuse existing)
    [SerializeField] private Button quitButton;
    // backButton removed — subpanels are closed via Resume or ToggleMenu
    [SerializeField] private Button resumeButton; // resume gameplay

    private bool isOpen = false;

    private void Start()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (highscoresPanel != null) highscoresPanel.SetActive(false);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        // backButton wiring removed

        // Try to auto-find Resume button inside the menu if not assigned in inspector
        if (resumeButton == null && menuPanel != null)
        {
            var childButtons = menuPanel.GetComponentsInChildren<Button>(true);
            foreach (var b in childButtons)
            {
                if (b == null) continue;
                if (b.name.ToLower().Contains("resume") || b.gameObject.name.ToLower().Contains("resume"))
                {
                    resumeButton = b;
                    Debug.Log($"[InGameMenu] Auto-found resume button: {b.name}");
                    break;
                }
            }
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(Resume);
            resumeButton.interactable = true;
            Debug.Log("[InGameMenu] Resume button wired.");
        }

        // Auto-find Settings and Highscores panels if they were copied into the menu and not assigned
        if (menuPanel != null)
        {
            if (settingsPanel == null)
            {
                foreach (var t in menuPanel.GetComponentsInChildren<Transform>(true))
                {
                    if (t == null) continue;
                    var n = t.name.ToLower();
                    if (n.Contains("setting") || n.Contains("options"))
                    {
                        settingsPanel = t.gameObject;
                        Debug.Log($"[InGameMenu] Auto-found settings panel: {t.name}");
                        break;
                    }
                }
            }

            if (highscoresPanel == null)
            {
                foreach (var t in menuPanel.GetComponentsInChildren<Transform>(true))
                {
                    if (t == null) continue;
                    var n = t.name.ToLower();
                    if (n.Contains("highscore") || n.Contains("score") || n.Contains("high_scores"))
                    {
                        highscoresPanel = t.gameObject;
                        Debug.Log($"[InGameMenu] Auto-found highscores panel: {t.name}");
                        break;
                    }
                }
            }

            // If still not found, try to find a top-level HighscoresPanel in the scene (prefab placed separately)
            if (highscoresPanel == null)
            {
                var found = GameObject.Find("HighscoresPanel");
                if (found != null)
                {
                    highscoresPanel = found;
                    Debug.Log("[InGameMenu] Found top-level HighscoresPanel in scene.");
                }
            }

            // Auto-wire buttons inside the menu to open the found panels
            var allButtons = menuPanel.GetComponentsInChildren<Button>(true);
            foreach (var b in allButtons)
            {
                if (b == null) continue;
                var n = b.name.ToLower();
                if ((n.Contains("setting") || n.Contains("settings") || n.Contains("options")) && settingsPanel != null)
                {
                    b.onClick.AddListener(OpenSettings);
                    Debug.Log($"[InGameMenu] Wired button '{b.name}' -> OpenSettings");
                }
                if ((n.Contains("high") || n.Contains("score")) && highscoresPanel != null)
                {
                    b.onClick.AddListener(OpenHighScores);
                    Debug.Log($"[InGameMenu] Wired button '{b.name}' -> OpenHighScores");
                }
            }
            
            // Wire Back buttons inside the found subpanels to return to the menu
            System.Func<GameObject, string, bool> wireBacks = (panelObj, panelName) =>
            {
                if (panelObj == null) return false;
                var btns = panelObj.GetComponentsInChildren<Button>(true);
                bool wiredAny = false;
                foreach (var bb in btns)
                {
                    if (bb == null) continue;
                    var bn = bb.name.ToLower();
                    if (bn.Contains("back") || bn.Contains("zurück") || bn.Contains("close") || bn.Contains("cancel") || bn.Contains("return") || bn.Contains("menu"))
                    {
                        bb.onClick.AddListener(BackToMenu);
                        bb.interactable = true;
                        Debug.Log($"[InGameMenu] Wired back button '{bb.name}' in {panelName} -> BackToMenu");
                        wiredAny = true;
                    }
                }
                return wiredAny;
            };

            if (settingsPanel != null)
                wireBacks(settingsPanel, "SettingsPanel");
            if (highscoresPanel != null)
                wireBacks(highscoresPanel, "HighscoresPanel");
        }
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ToggleMenu();
        }
#else
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }
#endif
    }

    public void ToggleMenu()
    {
        isOpen = !isOpen;
        if (menuPanel != null) menuPanel.SetActive(isOpen);

        if (isOpen)
        {
            Time.timeScale = 0f;
            Debug.Log("[InGameMenu] Menu opened - game paused");
        }
        else
        {
            CloseAllPanels();
            Time.timeScale = 1f;
            Debug.Log("[InGameMenu] Menu closed - game resumed");
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.DebugLogState();
            }
        }
    }

    // Resume called from Resume button - ensures timescale and UI state are restored
    public void Resume()
    {
        if (!isOpen)
            return;

        isOpen = false;
        if (menuPanel != null)
            menuPanel.SetActive(false);
        CloseAllPanels();
        Time.timeScale = 1f;
        Debug.Log("[InGameMenu] Resume button pressed - game resumed");
        if (WaveManager.Instance != null)
            WaveManager.Instance.DebugLogState();
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
        if (highscoresPanel != null)
            highscoresPanel.SetActive(false);
    }

    public void OpenHighScores()
    {
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
                // Fallback: try to find a TextMeshProUGUI inside the panel
                TextMeshProUGUI scoresText = highscoresPanel.GetComponentInChildren<TextMeshProUGUI>();
                if (scoresText != null && HighScoreManager.Instance != null)
                {
                    var scores = HighScoreManager.Instance.GetHighScores();
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
        }
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    private void CloseAllPanels()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (highscoresPanel != null) highscoresPanel.SetActive(false);
        if (menuPanel != null) menuPanel.SetActive(false);
        isOpen = false;
    }

    // Called by Back buttons inside subpanels to return to the main in-game menu
    public void BackToMenu()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (highscoresPanel != null) highscoresPanel.SetActive(false);
        if (menuPanel != null) menuPanel.SetActive(true);
        isOpen = true;
        Time.timeScale = 0f;
        Debug.Log("[InGameMenu] Back pressed - returning to menu");
    }

    private void QuitGame()
    {
        // Resume time before quitting
        Time.timeScale = 1f;
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
