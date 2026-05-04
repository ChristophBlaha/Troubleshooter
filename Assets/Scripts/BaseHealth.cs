using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BaseHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 50;
    [SerializeField] private Slider healthBar;
    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI finalWaveText;
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private Button submitScoreButton;
    [SerializeField] private TextMeshProUGUI submitConfirmationText;
    [SerializeField] private Button backToMenuButton;

    private int currentHealth;
    private bool isGameOver = false;

    private void Start()
    {
        if(gameOverScreen != null)
            gameOverScreen.SetActive(false);
        currentHealth = maxHealth;
        UpdateHealthBar();

        if (submitScoreButton != null)
        {
            submitScoreButton.onClick.AddListener(OnSubmitScore);
        }
        if (backToMenuButton != null)
        {
            backToMenuButton.onClick.AddListener(LoadMainMenu);
            // Make BackToMenu visible so player can return even without submitting
            backToMenuButton.gameObject.SetActive(true);
        }
        if (submitConfirmationText != null)
        {
            submitConfirmationText.gameObject.SetActive(false);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isGameOver) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }

        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.value = (float)currentHealth / maxHealth;
        }
    }

    private void Die()
    {
        isGameOver = true;
        Time.timeScale = 0f; // Pause game

        if (AudioManager.Instance)
        {
            AudioManager.Instance.PlaySFX("base_destroyed", 1f);
        }

        Debug.Log("Base destroyed!");
        
        // Show Game Over Screen
        if(gameOverScreen != null)
        {
            gameOverScreen.SetActive(true);
            DisplayFinalScore();
        }
    }

    private void DisplayFinalScore()
    {
        int finalScore = Score.Instance ? Score.Instance.GetScore() : 0;
        int finalWave = Score.Instance ? Score.Instance.GetCurrentWave() : 1;

        if (finalScoreText != null)
            finalScoreText.text = $"Final Score: {finalScore}";

        if (finalWaveText != null)
            finalWaveText.text = $"Waves Survived: {finalWave}";

        if (playerNameInput != null)
            if (string.IsNullOrEmpty(playerNameInput.text))
                playerNameInput.text = "Player";

        // Ensure confirmation / back button hidden until submit
        if (submitConfirmationText != null)
            submitConfirmationText.gameObject.SetActive(false);
        if (backToMenuButton != null)
            backToMenuButton.gameObject.SetActive(false);
    }

    private void OnSubmitScore()
    {
        if (!isGameOver) return;

        // Ensure a HighScoreManager exists (fallback: create one at runtime)
        if (HighScoreManager.Instance == null)
        {
            Debug.LogWarning("[BaseHealth] HighScoreManager.Instance is null - creating fallback manager at runtime.");
            var go = new GameObject("HighScoreManager");
            go.AddComponent<HighScoreManager>();
        }

        string playerName = playerNameInput != null ? playerNameInput.text : "Player";
        if (string.IsNullOrEmpty(playerName)) playerName = "Player";

        int finalScore = Score.Instance ? Score.Instance.GetScore() : 0;
        int finalWave = Score.Instance ? Score.Instance.GetCurrentWave() : 1;

        HighScoreManager.Instance.SaveScore(playerName, finalScore, finalWave);

        Debug.Log($"Score submitted: {playerName} - {finalScore}");

        // Show confirmation and enable back-to-menu button
        if (submitConfirmationText != null)
        {
            submitConfirmationText.text = "Score saved!";
            submitConfirmationText.gameObject.SetActive(true);
        }

        if (backToMenuButton != null)
        {
            backToMenuButton.gameObject.SetActive(true);
        }

        // disable submit to prevent double submits
        if (submitScoreButton != null)
            submitScoreButton.interactable = false;
    }

    private void LoadMainMenu()
    {
        Time.timeScale = 1f; // Resume time
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}