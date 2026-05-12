using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Settings Panel mit Volume Control
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TextMeshProUGUI masterVolumeText;
    [SerializeField] private TextMeshProUGUI musicVolumeText;
    [SerializeField] private TextMeshProUGUI sfxVolumeText;
    [SerializeField] private Button resetButton;
    [SerializeField] private Toggle mouseAsGazeToggle;

    private AudioManager audioManager;

    private void Start()
    {
        audioManager = AudioManager.Instance;
        
        // Initialisiere nach einem Frame, damit AudioManager sicher geladen ist
        StartCoroutine(InitializeSliders());
    }

    private IEnumerator InitializeSliders()
    {
        // Warte einen Frame, damit AudioManager seine Start() aufgerufen hat
        yield return null;

        if (audioManager == null)
        {
            audioManager = AudioManager.Instance;
        }

        if (audioManager == null)
        {
            Debug.LogError("[SettingsPanel] FEHLER: AudioManager nicht in Szene! Bitte AudioManager Prefab in die Szene ziehen.");
            yield break;
        }

        // Slider-Werte abrufen
        float masterVol = audioManager.GetMasterVolume();
        float musicVol = audioManager.GetMusicVolume();
        float sfxVol = audioManager.GetSFXVolume();

        // Slider-Setup
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.minValue = 0f;
            masterVolumeSlider.maxValue = 1f;
            masterVolumeSlider.value = masterVol;
            masterVolumeSlider.wholeNumbers = false;
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }
        
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.minValue = 0f;
            musicVolumeSlider.maxValue = 1f;
            musicVolumeSlider.value = musicVol;
            musicVolumeSlider.wholeNumbers = false;
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }
        
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.minValue = 0f;
            sfxVolumeSlider.maxValue = 1f;
            sfxVolumeSlider.value = sfxVol;
            sfxVolumeSlider.wholeNumbers = false;
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetToDefaults);
        }

        // Mouse-as-gaze toggle
        if (mouseAsGazeToggle != null)
        {
            bool enabled = PlayerPrefs.GetInt("UseMouseAsGaze", 0) == 1;
            if (TobiiManager.Instance != null)
               enabled = TobiiManager.Instance.IsMouseAsGazeEnabled();

            mouseAsGazeToggle.isOn = enabled;
            mouseAsGazeToggle.onValueChanged.AddListener(OnMouseAsGazeToggled);
        }

        UpdateDisplay();
    }

    private void OnMasterVolumeChanged(float value)
    {
        if (audioManager != null)
        {
            audioManager.SetMasterVolume(value);
            UpdateDisplay();
        }
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (audioManager != null)
        {
            audioManager.SetMusicVolume(value);
            UpdateDisplay();
        }
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (audioManager != null)
        {
            audioManager.SetSFXVolume(value);
            UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        if (audioManager == null)
            audioManager = AudioManager.Instance;

        // Lese von AudioManager statt vom Slider (sicherer)
        if (masterVolumeText != null && audioManager != null)
            masterVolumeText.text = $"Master: {(audioManager.GetMasterVolume() * 100):F0}%";
        else if (masterVolumeText != null)
            masterVolumeText.text = $"Master: {(masterVolumeSlider?.value ?? 0f) * 100:F0}%";

        if (musicVolumeText != null && audioManager != null)
            musicVolumeText.text = $"Music: {(audioManager.GetMusicVolume() * 100):F0}%";
        else if (musicVolumeText != null)
            musicVolumeText.text = $"Music: {(musicVolumeSlider?.value ?? 0f) * 100:F0}%";

        if (sfxVolumeText != null && audioManager != null)
            sfxVolumeText.text = $"SFX: {(audioManager.GetSFXVolume() * 100):F0}%";
        else if (sfxVolumeText != null)
            sfxVolumeText.text = $"SFX: {(sfxVolumeSlider?.value ?? 0f) * 100:F0}%";
    }

    public void ResetToDefaults()
    {
        if (audioManager != null)
        {
            audioManager.SetMasterVolume(1f);
            audioManager.SetMusicVolume(0.5f);
            audioManager.SetSFXVolume(0.8f);

            masterVolumeSlider.value = 1f;
            musicVolumeSlider.value = 0.5f;
            sfxVolumeSlider.value = 0.8f;

            UpdateDisplay();
            Debug.Log("Settings reset to defaults");
        }
    }

    private void OnMouseAsGazeToggled(bool enabled)
    {
        if (TobiiManager.Instance != null)
        {
            TobiiManager.Instance.SetUseMouseAsGaze(enabled);
        }
        else
        {
            PlayerPrefs.SetInt("UseMouseAsGaze", enabled ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
