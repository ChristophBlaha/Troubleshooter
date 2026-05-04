using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using Tobii.GameIntegration.Net;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

using TobiiGazePoint = Tobii.GameIntegration.Net.GazePoint;
using TobiiHeadPose = Tobii.GameIntegration.Net.HeadPose;

public class TobiiManager : MonoBehaviour
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    // ========================================================================
    // ÖFFENTLICHE DATEN
    // ========================================================================

    public static TobiiManager Instance { get; private set; }

    public Vector2 GazePointNormalized { get; private set; }
    public Vector2 GazePointViewport { get; private set; }
    public Vector3 HeadPosition { get; private set; }
    public Vector3 HeadRotation { get; private set; }
    public bool IsTrackerConnected { get; private set; }
    public bool IsUserPresent { get; private set; }
    public bool IsApiReady { get; private set; }
    public bool HasValidGazeData { get; private set; }
    public GameObject GazedObject { get; private set; }

    // ========================================================================
    // KONFIGURATION
    // ========================================================================

    [Header("Tobii Konfiguration")]
    [SerializeField] private string gameName = "MeinUnitySpiel";

    [Header("Gaze Stabilisierung")]
    [SerializeField] private float gazeGracePeriod = 0.15f;

    [Range(0f, 0.95f)]
    [SerializeField] private float gazeSmoothing = 0.3f;

    [Header("Fallback Input")]
    [Tooltip("Wenn aktiviert, wird die Mausposition als Gaze-Eingabe verwendet, falls Tobii nicht verfügbar oder zusätzlich.")]
    [SerializeField] private bool enableMouseAsGaze = false;
    // ========================================================================
    // PRIVATE FELDER
    // ========================================================================

    private static bool isDllLoaded = false;
    private float retryTimer = 0f;
    private float timeSinceLastGaze = 999f;
    private Vector2 smoothedGazeViewport;
    private bool everConnected = false;
    private const string PREF_MOUSE_AS_GAZE = "UseMouseAsGaze";
    private Camera cachedCamera;

    // ========================================================================
    // EDITOR CALLBACK
    // ========================================================================

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void RegisterEditorCallback()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            DoShutdown();
            Debug.Log("[Tobii] Shutdown via Editor-Callback");
        }
    }
#endif

    // ========================================================================
    // AWAKE
    // ========================================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        try
        {
            TobiiGameIntegrationApi.PrelinkAll();

            try { TobiiGameIntegrationApi.Shutdown(); }
            catch (Exception) { }

            TobiiGameIntegrationApi.SetApplicationName(gameName);
            isDllLoaded = true;

            Debug.Log("[Tobii] DLL geladen");
            IsApiReady = TobiiGameIntegrationApi.IsApiInitialized();
        }
        catch (Exception e)
        {
            Debug.LogError("[Tobii] Fehler: " + e.Message);
        }
        // Load preference for mouse fallback
        enableMouseAsGaze = PlayerPrefs.GetInt(PREF_MOUSE_AS_GAZE, enableMouseAsGaze ? 1 : 0) == 1;

        ResolveCamera(true);
        RebindTrackingWindow("Awake");
    }

    private void Start()
    {
        RebindTrackingWindow("Start");
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            RebindTrackingWindow("Focus");
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RebindTrackingWindow($"SceneLoaded:{scene.name}");
    }

    // ========================================================================
    // UPDATE
    // ========================================================================

    private void Update()
    {
        if (!isDllLoaded) return;

        TobiiGameIntegrationApi.Update();

        IsApiReady = TobiiGameIntegrationApi.IsApiInitialized();
        if (!IsApiReady) return;

        IsTrackerConnected = TobiiGameIntegrationApi.IsTrackerConnected();
        IsUserPresent = TobiiGameIntegrationApi.IsPresent();

        if (!IsTrackerConnected)
        {
            retryTimer += Time.deltaTime;
            if (retryTimer >= 2f)
            {
                retryTimer = 0f;
                RebindTrackingWindow("Retry");
            }
        }

        // Gaze Daten
        TobiiGazePoint gazePoint;
        bool freshData = TobiiGameIntegrationApi.TryGetLatestGazePoint(out gazePoint);

        if (freshData)
        {
            timeSinceLastGaze = 0f;

            GazePointNormalized = new Vector2(gazePoint.X, gazePoint.Y);

            Vector2 rawViewport = new Vector2(
                (gazePoint.X + 1f) * 0.5f,
                (gazePoint.Y + 1f) * 0.5f
            );

            if (gazeSmoothing > 0f && HasValidGazeData)
                smoothedGazeViewport = Vector2.Lerp(rawViewport, smoothedGazeViewport, gazeSmoothing);
            else
                smoothedGazeViewport = rawViewport;

            GazePointViewport = smoothedGazeViewport;
        }
        else
        {
            timeSinceLastGaze += Time.deltaTime;
        }

        HasValidGazeData = (timeSinceLastGaze <= gazeGracePeriod);

        // Wenn Mouse-as-Gaze aktiv ist, hat die Maus Vorrang vor Tobii-Daten.
        // Das ist kein reiner Fallback, sondern ein echter Eingabemodus.
        Camera cameraToUse = ResolveCamera(false);

        if (enableMouseAsGaze && cameraToUse != null)
        {
            if (!TryGetMouseScreenPosition(out Vector2 mouseScreenPos))
            {
                return;
            }

            Vector3 mousePos = mouseScreenPos;
            Vector2 mouseViewport = cameraToUse.ScreenToViewportPoint(mousePos);

            // Clamp to 0..1
            mouseViewport.x = Mathf.Clamp01(mouseViewport.x);
            mouseViewport.y = Mathf.Clamp01(mouseViewport.y);

            // Convert to Tobii normalized (-1..1) for compatibility
            GazePointNormalized = new Vector2(mouseViewport.x * 2f - 1f, mouseViewport.y * 2f - 1f);

            if (gazeSmoothing > 0f)
                smoothedGazeViewport = Vector2.Lerp(mouseViewport, smoothedGazeViewport, gazeSmoothing);
            else
                smoothedGazeViewport = mouseViewport;

            GazePointViewport = smoothedGazeViewport;
            HasValidGazeData = true;
        }

        // ====================================================================
        // 🔥 2D GAZE RAYCAST
        // ====================================================================

        GazedObject = null;

        cameraToUse = ResolveCamera(false);

        if (HasValidGazeData && cameraToUse != null)
        {
            Vector2 worldPos = cameraToUse.ViewportToWorldPoint(
                new Vector3(GazePointViewport.x, GazePointViewport.y, 0f));

            RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

            if (hit.collider != null)
            {
                GazedObject = hit.collider.gameObject;
            }
        }

        // Head Tracking (unverändert)
        TobiiHeadPose headPose;
        if (TobiiGameIntegrationApi.TryGetLatestHeadPose(out headPose))
        {
            HeadPosition = new Vector3(
                headPose.Position.X, headPose.Position.Y, headPose.Position.Z);

            HeadRotation = new Vector3(
                headPose.Rotation.YawDegrees,
                headPose.Rotation.PitchDegrees,
                headPose.Rotation.RollDegrees);
        }
    }

    // Public API to toggle mouse fallback at runtime
    public void SetUseMouseAsGaze(bool enabled)
    {
        enableMouseAsGaze = enabled;
        PlayerPrefs.SetInt(PREF_MOUSE_AS_GAZE, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public bool IsMouseAsGazeEnabled() => enableMouseAsGaze;

    // ========================================================================
    // SHUTDOWN
    // ========================================================================

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (Instance == this)
        {
            DoShutdown();
        }
    }

    private void OnApplicationQuit()
    {
        DoShutdown();
    }

    private static void DoShutdown()
    {
        if (isDllLoaded)
        {
            try
            {
                TobiiGameIntegrationApi.StopTracking();
                TobiiGameIntegrationApi.Shutdown();
            }
            catch (Exception) { }

            isDllLoaded = false;
            Debug.Log("[Tobii] Shutdown");
        }
    }

    private void RebindTrackingWindow(string reason)
    {
        if (!isDllLoaded)
            return;

        try
        {
            IntPtr hwnd = GetActiveWindow();
            if (hwnd != IntPtr.Zero)
            {
                TobiiGameIntegrationApi.TrackWindow(hwnd);
                retryTimer = 0f;
                Debug.Log($"[Tobii] TrackWindow ({reason})");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Tobii] TrackWindow fehlgeschlagen ({reason}): {e.Message}");
        }
    }

    private Camera ResolveCamera(bool refresh)
    {
        if (!refresh && cachedCamera != null)
            return cachedCamera;

        cachedCamera = Camera.main;

        if (cachedCamera == null)
        {
            cachedCamera = FindFirstObjectByType<Camera>();
        }

        return cachedCamera;
    }

    private bool TryGetMouseScreenPosition(out Vector2 screenPosition)
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        screenPosition = Input.mousePosition;
        return true;
#else
        screenPosition = default;
        return false;
#endif
    }
}