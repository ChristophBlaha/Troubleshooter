using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;
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

    [Header("Theme")]
    [SerializeField] private bool applyShooterTheme = true;

    private const string SubtitleName = "ThemeSubtitle";
    private const string FooterName = "ThemeFooter";
    private const string ThreatName = "ThemeThreat";
    private const string GridName = "ThemeGrid";
    private const string GlowName = "ThemeGlow";
    private const string HorizonName = "ThemeHorizon";
    private const string PanelBackdropName = "ThemePanelBackdrop";

    private static Sprite flatSprite;
    private static Sprite gridSprite;
    private static Sprite glowSprite;
    private static Sprite backgroundSprite;

    private static readonly Color HudInk = new Color32(226, 245, 255, 255);
    private static readonly Color HudMuted = new Color32(113, 174, 194, 255);
    private static readonly Color HudLine = new Color32(69, 188, 244, 210);
    private static readonly Color Alert = new Color32(255, 116, 73, 255);
    private static readonly Color PanelTint = new Color32(4, 20, 28, 244);

    private GameObject panelBackdrop;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI subtitleText;
    private TextMeshProUGUI footerText;
    private TextMeshProUGUI threatText;

    private void Awake()
    {
        if (applyShooterTheme)
            ApplyShooterTheme();
    }

    private void OnEnable()
    {
        if (applyShooterTheme)
            ApplyShooterTheme();
    }

    private void Start()
    {
        Time.timeScale = 1f;

        if (playButton != null)
            AssignButtonAction(playButton, PlayGame);

        if (settingsButton != null)
            AssignButtonAction(settingsButton, OpenSettings);

        if (highscoresButton != null)
            AssignButtonAction(highscoresButton, OpenHighScores);

        if (quitButton != null)
            AssignButtonAction(quitButton, QuitGame);

        if (backButton != null)
            AssignButtonAction(backButton, ClosePanel);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (highscoresPanel == null)
        {
            GameObject found = GameObject.Find("HighscoresPanel");
            if (found != null)
                highscoresPanel = found;
        }

        if (highscoresPanel != null)
            highscoresPanel.SetActive(false);

        if (applyShooterTheme)
            ApplyShooterTheme();

        SetSubpanelState(false);
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
        if (highscoresPanel != null)
            highscoresPanel.SetActive(false);

        SetSubpanelState(true);
    }

    public void OpenHighScores()
    {
        Debug.Log("Opening high scores...");
        if (highscoresPanel != null)
        {
            highscoresPanel.SetActive(true);
            HighscoresPanelUI panelUI = highscoresPanel.GetComponent<HighscoresPanelUI>();
            if (panelUI != null)
            {
                panelUI.UpdateFromManager();
            }
            else
            {
                UpdateHighScoresDisplay();
            }
        }

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        SetSubpanelState(true);
    }

    public void ClosePanel()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        if (highscoresPanel != null)
            highscoresPanel.SetActive(false);

        SetSubpanelState(false);
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

    private void UpdateHighScoresDisplay()
    {
        if (HighScoreManager.Instance == null)
            return;

        var scores = HighScoreManager.Instance.GetHighScores();
        TextMeshProUGUI scoresText = highscoresPanel?.GetComponentInChildren<TextMeshProUGUI>(true);
        if (scoresText == null)
            return;

        string text = "COMMAND SCOREBOARD\n\n";
        for (int i = 0; i < scores.Count; i++)
        {
            text += $"{i + 1}. {scores[i].playerName}  //  {scores[i].score}  //  WAVE {scores[i].wave}\n";
        }

        if (scores.Count == 0)
            text += "NO COMBAT RECORDS YET";

        scoresText.text = text;
        scoresText.alignment = TextAlignmentOptions.TopLeft;
    }

    private void ApplyShooterTheme()
    {
        RectTransform canvasRect = transform as RectTransform;
        if (canvasRect == null)
            return;

        Image background = transform.Find("Background")?.GetComponent<Image>();
        titleText = transform.Find("TitleText")?.GetComponent<TextMeshProUGUI>();

        ApplyBackgroundTheme(canvasRect, background);
        ApplyTitleTheme(canvasRect, titleText, out subtitleText, out footerText, out threatText);

        ApplyPrimaryButtonTheme(playButton, "DEPLOY", true);
        ApplyPrimaryButtonTheme(settingsButton, "SYSTEMS", false);
        ApplyPrimaryButtonTheme(highscoresButton, "WAR LOG", false);
        ApplyPrimaryButtonTheme(quitButton, "ABORT", false, true);

        ApplyPanelTheme(settingsPanel);
        ApplyPanelTheme(highscoresPanel);
        MatchPanelLayout(highscoresPanel, settingsPanel);

        if (backButton != null)
            ApplySecondaryButtonTheme(backButton, "RETURN");

        if (settingsPanel != null)
        {
            StyleSubpanelButtons(settingsPanel, "BACK");

            Slider[] sliders = settingsPanel.GetComponentsInChildren<Slider>(true);
            for (int i = 0; i < sliders.Length; i++)
                ApplySliderTheme(sliders[i]);

            Toggle[] toggles = settingsPanel.GetComponentsInChildren<Toggle>(true);
            for (int i = 0; i < toggles.Length; i++)
                ApplyToggleTheme(toggles[i]);
        }

        if (highscoresPanel != null)
        {
            StyleSubpanelButtons(highscoresPanel, "RETURN");
            ApplyScoreboardLayout(highscoresPanel);
        }

        int backdropSibling = canvasRect.childCount - 1;
        if (settingsPanel != null)
            backdropSibling = Mathf.Min(backdropSibling, settingsPanel.transform.GetSiblingIndex());
        if (highscoresPanel != null)
            backdropSibling = Mathf.Min(backdropSibling, highscoresPanel.transform.GetSiblingIndex());

        panelBackdrop = EnsurePanelBackdrop(canvasRect, backdropSibling);
        panelBackdrop.SetActive(false);
    }

    private void SetSubpanelState(bool panelOpen)
    {
        SetGraphicVisibility(playButton, !panelOpen);
        SetGraphicVisibility(settingsButton, !panelOpen);
        SetGraphicVisibility(highscoresButton, !panelOpen);
        SetGraphicVisibility(quitButton, !panelOpen);

        if (titleText != null)
            titleText.gameObject.SetActive(!panelOpen);
        if (subtitleText != null)
            subtitleText.gameObject.SetActive(!panelOpen);
        if (footerText != null)
            footerText.gameObject.SetActive(!panelOpen);
        if (threatText != null)
            threatText.gameObject.SetActive(!panelOpen);
        if (panelBackdrop != null)
            panelBackdrop.SetActive(panelOpen);
    }

    private static void SetGraphicVisibility(Button button, bool visible)
    {
        if (button == null)
            return;

        button.gameObject.SetActive(visible);
    }

    public static void ApplyPrimaryButtonTheme(Button button, string label, bool emphasize, bool danger = false)
    {
        if (button == null)
            return;

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = GetFlatSprite();
            image.type = Image.Type.Simple;
            image.color = emphasize ? new Color(0.03f, 0.03f, 0.1f, 0.96f) : new Color(0.02f, 0.02f, 0.04f, 0.92f);
        }

        ColorBlock colors = button.colors;
        colors.normalColor = image != null ? image.color : Color.white;
        colors.highlightedColor = emphasize ? new Color(0.08f, 0.12f, 0.22f, 1f) : new Color(0.07f, 0.12f, 0.16f, 0.98f);
        colors.pressedColor = new Color(0.03f, 0.05f, 0.08f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.14f, 0.14f, 0.16f, 0.45f);
        button.colors = colors;

        Outline outline = GetOrAdd<Outline>(button.gameObject);
        outline.effectColor = new Color(HudLine.r, HudLine.g, HudLine.b, 0.55f);
        outline.effectDistance = new Vector2(2f, -2f);

        Shadow shadow = GetOrAdd<Shadow>(button.gameObject);
        shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
        shadow.effectDistance = new Vector2(0f, -3f);

        Image accent = EnsureChildImage(button.transform, "ThemeAccent");
        accent.sprite = GetFlatSprite();
        accent.color = danger || emphasize ? Alert : HudLine;
        accent.raycastTarget = false;
        accent.rectTransform.anchorMin = new Vector2(0f, 0f);
        accent.rectTransform.anchorMax = new Vector2(0f, 1f);
        accent.rectTransform.pivot = new Vector2(0f, 0.5f);
        accent.rectTransform.sizeDelta = new Vector2(10f, 0f);
        accent.rectTransform.anchoredPosition = Vector2.zero;

        TextMeshProUGUI labelText = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (labelText != null)
        {
            labelText.text = label;
            labelText.fontSize = emphasize ? 28 : 25;
            labelText.fontStyle = FontStyles.Bold;
            labelText.characterSpacing = 8f;
            labelText.color = danger ? Alert : HudInk;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.enableVertexGradient = false;
        }
    }

    public static void ApplySecondaryButtonTheme(Button button, string label, bool danger = false)
    {
        if (button == null)
            return;

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = GetFlatSprite();
            image.type = Image.Type.Simple;
            image.color = new Color(0.03f, 0.04f, 0.08f, 0.95f);
        }

        ColorBlock colors = button.colors;
        colors.normalColor = image != null ? image.color : Color.white;
        colors.highlightedColor = new Color(0.08f, 0.12f, 0.18f, 0.98f);
        colors.pressedColor = new Color(0.03f, 0.04f, 0.07f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.14f, 0.14f, 0.16f, 0.45f);
        button.colors = colors;

        TextMeshProUGUI labelText = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (labelText != null)
        {
            labelText.text = label;
            labelText.fontStyle = FontStyles.Bold;
            labelText.characterSpacing = 6f;
            labelText.color = danger ? Alert : HudInk;
            labelText.alignment = TextAlignmentOptions.Center;
        }
    }

    public static void ApplyPanelTheme(GameObject panel)
    {
        if (panel == null)
            return;

        Image panelImage = panel.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.sprite = GetFlatSprite();
            panelImage.type = Image.Type.Simple;
            panelImage.color = PanelTint;
        }

        Outline outline = GetOrAdd<Outline>(panel);
        outline.effectColor = new Color(HudLine.r, HudLine.g, HudLine.b, 0.45f);
        outline.effectDistance = new Vector2(2f, -2f);

        Shadow shadow = GetOrAdd<Shadow>(panel);
        shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        shadow.effectDistance = new Vector2(0f, -4f);
    }

    public static void StyleSubpanelButtons(GameObject panel, string defaultLabel)
    {
        if (panel == null)
            return;

        Button[] buttons = panel.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            string lowerName = buttons[i].name.ToLower();
            bool isReset = lowerName.Contains("reset");
            ApplySecondaryButtonTheme(buttons[i], isReset ? "RESET" : defaultLabel, isReset);
        }
    }

    public static void MatchPanelLayout(GameObject targetPanel, GameObject referencePanel)
    {
        if (targetPanel == null || referencePanel == null)
            return;

        RectTransform targetRect = targetPanel.transform as RectTransform;
        RectTransform referenceRect = referencePanel.transform as RectTransform;
        if (targetRect == null || referenceRect == null)
            return;

        targetRect.anchorMin = referenceRect.anchorMin;
        targetRect.anchorMax = referenceRect.anchorMax;
        targetRect.pivot = referenceRect.pivot;
        targetRect.anchoredPosition = referenceRect.anchoredPosition;
        targetRect.sizeDelta = referenceRect.sizeDelta;
        targetRect.localScale = referenceRect.localScale;
        targetRect.localRotation = referenceRect.localRotation;
    }

    public static void ApplyScoreboardLayout(GameObject panel)
    {
        if (panel == null)
            return;

        TextMeshProUGUI scoresText = panel.GetComponentInChildren<TextMeshProUGUI>(true);
        if (scoresText == null)
            return;

        RectTransform textRect = scoresText.rectTransform;
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.offsetMin = new Vector2(72f, 84f);
        textRect.offsetMax = new Vector2(-72f, -92f);

        scoresText.alignment = TextAlignmentOptions.TopLeft;
        scoresText.fontSize = 28f;
        scoresText.fontStyle = FontStyles.Bold;
        scoresText.color = HudInk;
        scoresText.characterSpacing = 1f;
        scoresText.lineSpacing = 8f;
        scoresText.enableAutoSizing = false;
        scoresText.enableWordWrapping = true;
        scoresText.overflowMode = TextOverflowModes.Overflow;
    }

    public static void ApplySliderTheme(Slider slider)
    {
        if (slider == null)
            return;

        if (slider.fillRect != null)
        {
            Image fill = slider.fillRect.GetComponent<Image>();
            if (fill != null)
                fill.color = new Color(0.27f, 0.76f, 0.93f, 1f);
        }

        if (slider.handleRect != null)
        {
            Image handle = slider.handleRect.GetComponent<Image>();
            if (handle != null)
                handle.color = Alert;
        }
    }

    public static void ApplyToggleTheme(Toggle toggle)
    {
        if (toggle == null)
            return;

        if (toggle.targetGraphic is Image target)
            target.color = new Color(0.06f, 0.14f, 0.2f, 1f);

        if (toggle.graphic is Image checkmark)
            checkmark.color = new Color(0.22f, 0.85f, 1f, 1f);
    }

    private static void ApplyBackgroundTheme(RectTransform canvasRect, Image background)
    {
        if (canvasRect == null || background == null)
            return;

        RectTransform rect = background.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.SetAsFirstSibling();

        background.sprite = GetBackgroundSprite();
        background.type = Image.Type.Simple;
        background.color = Color.white;
        background.raycastTarget = false;

        Image glow = EnsureImage(canvasRect, GlowName, 1);
        Stretch(glow.rectTransform);
        glow.sprite = GetGlowSprite();
        glow.color = new Color(0.33f, 0.97f, 1f, 0.12f);
        glow.raycastTarget = false;

        Image grid = EnsureImage(canvasRect, GridName, 2);
        Stretch(grid.rectTransform);
        grid.sprite = GetGridSprite();
        grid.color = new Color(0.27f, 0.78f, 0.95f, 0.18f);
        grid.raycastTarget = false;

        Image horizon = EnsureImage(canvasRect, HorizonName, 3);
        horizon.sprite = GetFlatSprite();
        horizon.color = new Color(1f, 0.43f, 0.22f, 0.14f);
        horizon.raycastTarget = false;
        horizon.rectTransform.anchorMin = new Vector2(0f, 0.2f);
        horizon.rectTransform.anchorMax = new Vector2(1f, 0.26f);
        horizon.rectTransform.offsetMin = Vector2.zero;
        horizon.rectTransform.offsetMax = Vector2.zero;
    }

    private static void ApplyTitleTheme(
        RectTransform canvasRect,
        TextMeshProUGUI titleText,
        out TextMeshProUGUI subtitle,
        out TextMeshProUGUI footer,
        out TextMeshProUGUI threat)
    {
        subtitle = null;
        footer = null;
        threat = null;

        if (canvasRect == null || titleText == null)
            return;

        titleText.text = "TROUBLESHOOTER";
        titleText.fontSize = 56;
        titleText.fontStyle = FontStyles.Bold;
        titleText.characterSpacing = 2f;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = HudInk;
        titleText.enableVertexGradient = true;
        titleText.colorGradient = new VertexGradient(
            new Color32(255, 255, 255, 255),
            new Color32(255, 255, 255, 255),
            new Color32(118, 217, 255, 255),
            new Color32(118, 217, 255, 255));
        titleText.enableWordWrapping = false;
        titleText.overflowMode = TextOverflowModes.Overflow;
        titleText.enableAutoSizing = true;
        titleText.fontSizeMin = 34;
        titleText.fontSizeMax = 54;
        titleText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        titleText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        titleText.rectTransform.pivot = new Vector2(0.5f, 1f);
        titleText.rectTransform.sizeDelta = new Vector2(1080f, 72f);
        titleText.rectTransform.anchoredPosition = new Vector2(0f, -34f);

        subtitle = EnsureText(canvasRect, SubtitleName, 4);
        subtitle.gameObject.SetActive(false);
        subtitle = null;

        footer = EnsureText(canvasRect, FooterName, canvasRect.childCount - 1);
        footer.gameObject.SetActive(false);
        footer = null;

        threat = EnsureText(canvasRect, ThreatName, canvasRect.childCount - 1);
        threat.gameObject.SetActive(false);
        threat = null;
    }

    private static GameObject EnsurePanelBackdrop(RectTransform canvasRect, int siblingIndex)
    {
        Image image = EnsureImage(canvasRect, PanelBackdropName, siblingIndex);
        Stretch(image.rectTransform);
        image.sprite = GetFlatSprite();
        image.color = new Color(0f, 0.02f, 0.04f, 0.55f);
        image.raycastTarget = true;
        return image.gameObject;
    }

    private static Button FindNamedButton(Transform root, string name)
    {
        if (root == null)
            return null;

        Transform found = root.Find(name);
        if (found != null)
            return found.GetComponent<Button>();

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].name == name)
                return buttons[i];
        }

        return null;
    }

    private static void AssignButtonAction(Button button, UnityAction action)
    {
        if (button == null || action == null)
            return;

        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(action);
        button.interactable = true;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T existing = target.GetComponent<T>();
        if (existing != null)
            return existing;

        return target.AddComponent<T>();
    }

    private static Image EnsureImage(RectTransform parent, string name, int siblingIndex)
    {
        Transform existing = parent.Find(name);
        Image image;

        if (existing == null)
        {
            GameObject child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            child.transform.SetParent(parent, false);
            image = child.GetComponent<Image>();
        }
        else
        {
            image = existing.GetComponent<Image>();
        }

        image.transform.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, parent.childCount - 1));
        return image;
    }

    private static Image EnsureChildImage(Transform parent, string name)
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

    private static TextMeshProUGUI EnsureText(RectTransform parent, string name, int siblingIndex)
    {
        Transform existing = parent.Find(name);
        TextMeshProUGUI text;

        if (existing == null)
        {
            GameObject child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            child.transform.SetParent(parent, false);
            text = child.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            text = existing.GetComponent<TextMeshProUGUI>();
        }

        text.transform.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, parent.childCount - 1));
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Sprite GetFlatSprite()
    {
        if (flatSprite != null)
            return flatSprite;

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        flatSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        return flatSprite;
    }

    private static Sprite GetGridSprite()
    {
        if (gridSprite != null)
            return gridSprite;

        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool majorLine = x % 32 == 0 || y % 32 == 0;
                bool minorLine = x % 16 == 0 || y % 16 == 0;
                float alpha = majorLine ? 0.45f : (minorLine ? 0.14f : 0f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Point;
        gridSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return gridSprite;
    }

    private static Sprite GetGlowSprite()
    {
        if (glowSprite != null)
            return glowSprite;

        const int size = 256;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(0.5f, 0.62f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = x / (size - 1f);
                float v = y / (size - 1f);
                float dx = (u - center.x) / 0.55f;
                float dy = (v - center.y) / 0.38f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - dist);
                alpha = alpha * alpha * 0.95f;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        glowSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return glowSprite;
    }

    private static Sprite GetBackgroundSprite()
    {
        if (backgroundSprite != null)
            return backgroundSprite;

        const int width = 256;
        const int height = 256;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        for (int y = 0; y < height; y++)
        {
            float v = y / (height - 1f);
            Color baseColor = Color.Lerp(new Color(0.01f, 0.02f, 0.05f), new Color(0.05f, 0.1f, 0.14f), Mathf.Pow(v, 0.8f));

            for (int x = 0; x < width; x++)
            {
                float u = x / (width - 1f);
                float wave = Mathf.Sin(u * 18f + v * 9f) * 0.5f + 0.5f;
                float noise = Mathf.Sin((u + 0.21f) * 29f) * Mathf.Cos((v + 0.07f) * 21f);
                noise = noise * 0.5f + 0.5f;
                float star = Hash(x, y) > 0.9955f ? 1f : 0f;

                Color color = baseColor;
                color += new Color(0f, 0.08f, 0.11f, 0f) * (wave * 0.22f);
                color += new Color(0.08f, 0.06f, 0.02f, 0f) * Mathf.Clamp01((1f - v) * 0.28f);
                color += new Color(0f, 0.03f, 0.06f, 0f) * (noise * 0.12f);
                color += new Color(1f, 1f, 1f, 0f) * star;
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        backgroundSprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        return backgroundSprite;
    }

    private static float Hash(int x, int y)
    {
        float value = Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f;
        return value - Mathf.Floor(value);
    }
}
