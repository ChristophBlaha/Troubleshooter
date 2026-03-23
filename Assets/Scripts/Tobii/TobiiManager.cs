using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Tobii.GameIntegration.Net;

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

    // ========================================================================
    // PRIVATE FELDER
    // ========================================================================

    private static bool isDllLoaded = false;
    private float retryTimer = 0f;
    private float timeSinceLastGaze = 999f;
    private Vector2 smoothedGazeViewport;
    private bool everConnected = false;

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
                IntPtr hwnd = GetActiveWindow();
                TobiiGameIntegrationApi.TrackWindow(hwnd);
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

        // ====================================================================
        // 🔥 2D GAZE RAYCAST
        // ====================================================================

        GazedObject = null;

        if (HasValidGazeData && Camera.main != null)
        {
            Vector2 worldPos = Camera.main.ViewportToWorldPoint(
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

    // ========================================================================
    // SHUTDOWN
    // ========================================================================

    private void OnDisable()
    {
        DoShutdown();
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
}