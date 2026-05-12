using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Zentrale Audio-Verwaltung
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [System.Serializable]
    public class AudioClipReference
    {
        public string id;
        public AudioClip clip;
    }

    [SerializeField] private AudioClipReference[] soundEffects;
    [SerializeField] private AudioClip backgroundMusic;
    
    [SerializeField] private float masterVolume = 1f;
    [SerializeField] private float sfxVolume = 0.8f;
    [SerializeField] private float musicVolume = 0.5f;

    private Dictionary<string, AudioClip> sfxDictionary = new Dictionary<string, AudioClip>();
    private AudioSource musicSource;
    private List<AudioSource> sfxSources = new List<AudioSource>();
    private const int MAX_SFX_SOURCES = 8;

    private void Awake()
    {
        Debug.Log($"[AudioManager] Awake() aufgerufen. Existierende Instance: {(Instance != null ? Instance.name : "NULL")}");

        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[AudioManager] Mehrfache Instanz erkannt! Zerstöre diese: {gameObject.name}");
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[AudioManager] Singleton initialisiert und DontDestroyOnLoad gesetzt!");
    }

    public static AudioManager GetOrCreate()
    {
        if (Instance == null)
        {
            Debug.LogError("[AudioManager] AudioManager nicht in Szene! Bitte AudioManager Prefab hinzufügen.");
            return null;
        }
        return Instance;
    }

    private void Start()
    {
        Debug.Log("[AudioManager] Start() aufgerufen");
        
        // Nur initialisieren wenn noch nicht geschehen
        if (musicSource == null)
        {
            InitializeSoundDictionary();
            InitializeAudioSources();
            PlayMusic(backgroundMusic);
            LoadVolumeSettings();
            Debug.Log("[AudioManager] Initialisierung abgeschlossen");
        }
    }

    private void InitializeSoundDictionary()
    {
        if (soundEffects == null || soundEffects.Length == 0)
        {
            Debug.LogWarning("[AudioManager] soundEffects Array ist null oder leer! Bitte im Prefab konfigurieren.");
            return;
        }

        foreach (var sfx in soundEffects)
        {
            if (sfx != null && !string.IsNullOrEmpty(sfx.id) && sfx.clip != null)
            {
                if (!sfxDictionary.ContainsKey(sfx.id))
                {
                    sfxDictionary.Add(sfx.id, sfx.clip);
                }
            }
        }
    }

    private void InitializeAudioSources()
    {
        // Music source
        GameObject musicGO = new GameObject("MusicSource");
        musicGO.transform.SetParent(transform);
        musicSource = musicGO.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.volume = musicVolume * masterVolume;

        // SFX sources (pool)
        for (int i = 0; i < MAX_SFX_SOURCES; i++)
        {
            GameObject sfxGO = new GameObject($"SFXSource_{i}");
            sfxGO.transform.SetParent(transform);
            AudioSource sfxSource = sfxGO.AddComponent<AudioSource>();
            sfxSource.volume = sfxVolume * masterVolume;
            sfxSources.Add(sfxSource);
        }
    }

    public void PlaySFX(string sfxId, float volumeMultiplier = 1f, float pitch = 1f)
    {
        string resolvedId = ResolveSfxId(sfxId);
        if (!sfxDictionary.ContainsKey(resolvedId))
        {
            Debug.LogWarning($"[AudioManager] SFX nicht gefunden: {sfxId}");
            return;
        }

        AudioSource availableSource = GetAvailableSFXSource();
        if (availableSource != null)
        {
            availableSource.clip = sfxDictionary[resolvedId];
            availableSource.volume = sfxVolume * masterVolume * volumeMultiplier;
            availableSource.pitch = pitch;
            availableSource.PlayOneShot(availableSource.clip);
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        if (musicSource == null) return;
        
        musicSource.clip = clip;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
    }

    public float GetMasterVolume() => masterVolume;
    public float GetSFXVolume() => sfxVolume;
    public float GetMusicVolume() => musicVolume;

    private void UpdateAllVolumes()
    {
        if (musicSource != null)
            musicSource.volume = musicVolume * masterVolume;
        
        if (sfxSources != null)
        {
            foreach (var source in sfxSources)
            {
                if (source != null)
                    source.volume = sfxVolume * masterVolume;
            }
        }
    }

    private void LoadVolumeSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
        
        // Sicherheitsfallback: wenn Werte irgendwie auf 0 sind, setze auf Defaults
        if (masterVolume <= 0f) masterVolume = 1f;
        if (sfxVolume <= 0f) sfxVolume = 0.8f;
        if (musicVolume <= 0f) musicVolume = 0.5f;
        
        Debug.Log($"[AudioManager] Lautstärken geladen: Master={masterVolume}, Music={musicVolume}, SFX={sfxVolume}");
        UpdateAllVolumes();
    }

    private AudioSource GetAvailableSFXSource()
    {
        if (sfxSources == null || sfxSources.Count == 0)
        {
            Debug.LogWarning("[AudioManager] SFX Sources nicht initialisiert!");
            return null;
        }

        foreach (var source in sfxSources)
        {
            if (source != null && !source.isPlaying)
            {
                return source;
            }
        }
        // Wenn alle belegt, nutze den ältesten
        return sfxSources[0];
    }

    private string ResolveSfxId(string sfxId)
    {
        if (sfxDictionary.ContainsKey(sfxId))
            return sfxId;

        if (sfxId == "ally_returned_home" && sfxDictionary.ContainsKey("ally_arrived"))
            return "ally_arrived";

        return sfxId;
    }
}
