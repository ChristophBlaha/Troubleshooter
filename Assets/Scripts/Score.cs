using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Score : MonoBehaviour
{
    public const int EnemyKillPoints = 5;
    public const int FriendlyReachedBasePoints = 5;
    public const int FriendlyRescuedPoints = 10;
    public const int FriendlyKilledPenalty = -10;
    public const int WaveSurvivedPoints = 20;

    private int score;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI waveText;
    public static Score Instance { get; private set; }
    
    private WaveManager waveManager;
    private TextMeshProUGUI waveBannerText;
    private CanvasGroup waveBannerGroup;
    private Coroutine waveAnnouncementRoutine;

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
            waveManager.OnWaveStart.AddListener(HandleWaveStart);
            waveManager.OnWaveComplete.AddListener(HandleWaveComplete);
        }
        ApplyHudTheme();
        RefreshScoreDisplay();
        UpdateWaveDisplay(waveManager != null ? waveManager.GetCurrentWave() : 1);
    }

    public void IncreaseScore(int amount)
    {
        score += amount;
        RefreshScoreDisplay();
        if (AudioManager.Instance)
        {
            AudioManager.Instance.PlaySFX("score_gained", 0.6f);
        }
        Debug.Log($"Score increased: {amount}, Total: {score}");
    }

    public void AddEnemyKillScore() => IncreaseScore(EnemyKillPoints);
    public void AddFriendlyReachedBaseScore() => IncreaseScore(FriendlyReachedBasePoints);
    public void AddFriendlyRescuedScore() => IncreaseScore(FriendlyRescuedPoints);
    public void AddFriendlyKilledPenalty() => IncreaseScore(FriendlyKilledPenalty);
    public void AddWaveSurvivedScore() => IncreaseScore(WaveSurvivedPoints);

    private void UpdateWaveDisplay(int waveNumber)
    {
        if (waveText != null)
        {
            waveText.text = $"WAVE // {waveNumber}";
        }
    }

    private void HandleWaveStart(int waveNumber)
    {
        UpdateWaveDisplay(waveNumber);
        PlayWaveAnnouncement(waveNumber);
    }

    private void HandleWaveComplete(int waveNumber)
    {
        AddWaveSurvivedScore();
    }
    
    public int GetScore()
    {
        return score;
    }

    public int GetCurrentWave()
    {
        return waveManager ? waveManager.GetCurrentWave() : 1;
    }

    private void RefreshScoreDisplay()
    {
        if (scoreText != null)
            scoreText.text = $"SCORE // {score}";
    }

    private void ApplyHudTheme()
    {
        if (Camera.main != null)
            Camera.main.backgroundColor = new Color(0.02f, 0.05f, 0.09f, 1f);

        if (scoreText != null)
        {
            RectTransform scoreRect = scoreText.rectTransform;
            scoreRect.anchorMin = new Vector2(1f, 1f);
            scoreRect.anchorMax = new Vector2(1f, 1f);
            scoreRect.pivot = new Vector2(1f, 1f);
            scoreRect.anchoredPosition = new Vector2(-26f, -20f);
            scoreRect.sizeDelta = new Vector2(280f, 42f);

            scoreText.fontSize = 24;
            scoreText.fontStyle = FontStyles.Bold;
            scoreText.characterSpacing = 2f;
            scoreText.alignment = TextAlignmentOptions.Right;
            scoreText.color = new Color32(226, 245, 255, 255);
        }

        if (waveText != null)
        {
            RectTransform waveRect = waveText.rectTransform;
            waveRect.anchorMin = new Vector2(1f, 1f);
            waveRect.anchorMax = new Vector2(1f, 1f);
            waveRect.pivot = new Vector2(1f, 1f);
            waveRect.anchoredPosition = new Vector2(-26f, -60f);
            waveRect.sizeDelta = new Vector2(280f, 28f);

            waveText.fontSize = 15;
            waveText.fontStyle = FontStyles.Bold;
            waveText.characterSpacing = 1.5f;
            waveText.alignment = TextAlignmentOptions.Right;
            waveText.color = new Color32(113, 174, 194, 255);
        }

        if (scoreText != null)
        {
            RectTransform parentRect = scoreText.transform.parent as RectTransform;
            if (parentRect != null)
            {
                Image plate = EnsureImage(parentRect, "HudScorePlate");
                plate.color = new Color(0.02f, 0.04f, 0.08f, 0.74f);
                plate.raycastTarget = false;

                RectTransform plateRect = plate.rectTransform;
                plateRect.anchorMin = new Vector2(1f, 1f);
                plateRect.anchorMax = new Vector2(1f, 1f);
                plateRect.pivot = new Vector2(1f, 1f);
                plateRect.anchoredPosition = new Vector2(-18f, -14f);
                plateRect.sizeDelta = new Vector2(300f, 84f);
                plateRect.SetAsFirstSibling();

                Outline outline = plate.GetComponent<Outline>();
                if (outline == null)
                    outline = plate.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.27f, 0.76f, 0.93f, 0.42f);
                outline.effectDistance = new Vector2(2f, -2f);

                Image accent = EnsureImage(parentRect, "HudScoreAccent");
                accent.color = new Color(1f, 0.45f, 0.27f, 1f);
                accent.raycastTarget = false;

                RectTransform accentRect = accent.rectTransform;
                accentRect.anchorMin = new Vector2(1f, 1f);
                accentRect.anchorMax = new Vector2(1f, 1f);
                accentRect.pivot = new Vector2(1f, 1f);
                accentRect.anchoredPosition = new Vector2(-18f, -14f);
                accentRect.sizeDelta = new Vector2(7f, 84f);
                accentRect.SetSiblingIndex(plateRect.GetSiblingIndex() + 1);
            }
        }

        EnsureWaveBanner();
    }

    private Image EnsureImage(RectTransform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing == null)
        {
            GameObject child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            child.transform.SetParent(parent, false);
            return child.GetComponent<Image>();
        }

        return existing.GetComponent<Image>();
    }

    private void EnsureWaveBanner()
    {
        if (waveText == null || waveBannerText != null)
            return;

        RectTransform parentRect = waveText.transform.parent as RectTransform;
        if (parentRect == null)
            return;

        GameObject bannerObject = new GameObject("WaveBanner", typeof(RectTransform), typeof(CanvasRenderer), typeof(CanvasGroup), typeof(TextMeshProUGUI));
        bannerObject.transform.SetParent(parentRect, false);

        RectTransform bannerRect = bannerObject.GetComponent<RectTransform>();
        bannerRect.anchorMin = new Vector2(0.5f, 1f);
        bannerRect.anchorMax = new Vector2(0.5f, 1f);
        bannerRect.pivot = new Vector2(0.5f, 1f);
        bannerRect.anchoredPosition = new Vector2(0f, -32f);
        bannerRect.sizeDelta = new Vector2(460f, 84f);

        waveBannerGroup = bannerObject.GetComponent<CanvasGroup>();
        waveBannerGroup.alpha = 0f;

        waveBannerText = bannerObject.GetComponent<TextMeshProUGUI>();
        waveBannerText.text = string.Empty;
        waveBannerText.font = waveText.font;
        waveBannerText.fontSize = 34;
        waveBannerText.fontStyle = FontStyles.Bold;
        waveBannerText.alignment = TextAlignmentOptions.Center;
        waveBannerText.color = new Color32(226, 245, 255, 255);
        waveBannerText.characterSpacing = 3f;
    }

    private void PlayWaveAnnouncement(int waveNumber)
    {
        if (waveText == null)
            return;

        EnsureWaveBanner();

        if (waveAnnouncementRoutine != null)
            StopCoroutine(waveAnnouncementRoutine);

        waveAnnouncementRoutine = StartCoroutine(AnimateWaveAnnouncement(waveNumber));
    }

    private IEnumerator AnimateWaveAnnouncement(int waveNumber)
    {
        Vector3 originalScale = waveText.rectTransform.localScale;
        Color originalColor = waveText.color;
        Color flashColor = new Color32(255, 126, 84, 255);

        if (waveBannerText != null && waveBannerGroup != null)
        {
            waveBannerText.text = $"WAVE {waveNumber} INBOUND";
            waveBannerGroup.alpha = 0f;
            waveBannerText.rectTransform.localScale = Vector3.one * 0.88f;
        }

        float duration = 1.15f;
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float pulse = Mathf.Sin(t * Mathf.PI);

            waveText.color = Color.Lerp(originalColor, flashColor, pulse);
            waveText.rectTransform.localScale = Vector3.Lerp(originalScale, originalScale * 1.35f, pulse);

            if (waveBannerText != null && waveBannerGroup != null)
            {
                waveBannerGroup.alpha = Mathf.Clamp01(pulse * 1.3f);
                waveBannerText.rectTransform.localScale = Vector3.Lerp(Vector3.one * 0.88f, Vector3.one, pulse);
            }

            yield return null;
        }

        waveText.color = originalColor;
        waveText.rectTransform.localScale = originalScale;

        if (waveBannerGroup != null)
            waveBannerGroup.alpha = 0f;

        waveAnnouncementRoutine = null;
    }
}
