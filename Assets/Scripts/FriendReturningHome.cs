using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FriendReturningHome : MonoBehaviour
{
    [SerializeField] private GameObject alliedDefenderPrefab;
    [SerializeField] private float spawnOffsetDistance = 2f;

    private Rigidbody2D rb;
    private EnemyMovement movement;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<EnemyMovement>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Base"))
            return;

        // Score
        Score.Instance.IncreaseScore(10);

        // Audio
        if (AudioManager.Instance)
        {
            AudioManager.Instance.PlaySFX("ally_arrived", 0.7f);
        }

        // Spawn AlliedDefender neben Base
        SpawnAlliedDefender(collision.gameObject.transform);

        // Melde diesen Gegner als gestorben für Wave-Zählung
        if (WaveController.Instance != null)
        {
            WaveController.Instance.RegisterEnemyDeath();
        }

        Destroy(this.gameObject);
    }

    private void SpawnAlliedDefender(Transform baseTransform)
    {
        if (alliedDefenderPrefab == null)
        {
            Debug.LogWarning("AlliedDefenderPrefab nicht gesetzt auf FriendReturningHome!");
            return;
        }

        // Position neben Base spawnen
        Vector3 spawnOffset = Random.onUnitSphere * spawnOffsetDistance;
        spawnOffset.z = 0;
        Vector3 spawnPos = baseTransform.position + spawnOffset;

        GameObject defender = Instantiate(alliedDefenderPrefab, spawnPos, Quaternion.identity);
        Debug.Log($"Allied Defender spawned at {spawnPos}");
    }
}