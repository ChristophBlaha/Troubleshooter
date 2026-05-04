using UnityEngine;

/// <summary>
/// Game Initializer - Stellt sicher, dass alle Manager existieren
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [SerializeField] private GameObject audioManagerPrefab;
    [SerializeField] private GameObject highScoreManagerPrefab;

    private void Awake()
    {
        // AudioManager
        if (AudioManager.Instance == null && audioManagerPrefab != null)
        {
            Instantiate(audioManagerPrefab);
            Debug.Log("[GameInitializer] AudioManager instantiated");
        }

        // HighScoreManager
        if (HighScoreManager.Instance == null && highScoreManagerPrefab != null)
        {
            Instantiate(highScoreManagerPrefab);
            Debug.Log("[GameInitializer] HighScoreManager instantiated");
        }
    }
}
