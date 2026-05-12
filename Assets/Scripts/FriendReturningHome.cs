using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FriendReturningHome : MonoBehaviour
{
    [SerializeField] private GameObject alliedDefenderPrefab;
    [SerializeField] private float tangentSpawnSpacing = 0.95f;
    [SerializeField] private float dockOffsetFromBase = 0.18f;
    [SerializeField] private float minimumDockSeparation = 0.8f;
    [SerializeField] private int maxDockLaneSearch = 4;

    private Rigidbody2D rb;
    private EnemyMovement movement;
    private Collider2D[] colliders;
    private Renderer[] renderers;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<EnemyMovement>();
        colliders = GetComponentsInChildren<Collider2D>(true);
        renderers = GetComponentsInChildren<Renderer>(true);
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

        PrepareIncomingFriendlyForRescue();

        // Spawn AlliedDefender neben Base
        SpawnAlliedDefender(collision);

        // Melde diesen Gegner als gestorben für Wave-Zählung
        if (WaveController.Instance != null)
        {
            WaveController.Instance.RegisterEnemyDeath();
        }

        Destroy(this.gameObject);
    }

    private void SpawnAlliedDefender(Collision2D collision)
    {
        if (alliedDefenderPrefab == null)
        {
            Debug.LogWarning("AlliedDefenderPrefab nicht gesetzt auf FriendReturningHome!");
            return;
        }

        Transform baseTransform = collision.transform;
        Vector3 spawnPos = GetRescueTouchPosition(collision, baseTransform);
        Vector2 launchDirection = ((Vector2)spawnPos - (Vector2)baseTransform.position).normalized;
        if (launchDirection.sqrMagnitude < 0.0001f)
            launchDirection = Vector2.up;

        GameObject defender = Instantiate(alliedDefenderPrefab, spawnPos, Quaternion.identity);
        AlliedDefender alliedDefender = defender.GetComponent<AlliedDefender>();
        if (alliedDefender != null)
            alliedDefender.InitializeFromRescue(baseTransform, launchDirection);

        Debug.Log($"Allied Defender spawned at {spawnPos}");
    }

    private void PrepareIncomingFriendlyForRescue()
    {
        if (movement != null)
            movement.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }

        if (colliders != null)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                    colliders[i].enabled = false;
            }
        }

        if (renderers != null)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].enabled = false;
            }
        }
    }

    private Vector3 GetRescueTouchPosition(Collision2D collision, Transform baseTransform)
    {
        Vector2 basePosition = baseTransform.position;
        Vector2 outwardDirection = ((Vector2)transform.position - basePosition).normalized;
        if (outwardDirection.sqrMagnitude < 0.0001f)
            outwardDirection = Vector2.up;

        Vector2 tangentDirection = new Vector2(-outwardDirection.y, outwardDirection.x);
        Vector2 contactPoint = collision.contactCount > 0 ? collision.GetContact(0).point : (Vector2)transform.position;

        Vector2 fallbackPosition = contactPoint + outwardDirection * dockOffsetFromBase;
        for (int laneStep = 0; laneStep <= maxDockLaneSearch; laneStep++)
        {
            int candidateCount = laneStep == 0 ? 1 : 2;
            for (int i = 0; i < candidateCount; i++)
            {
                int laneIndex = laneStep == 0 ? 0 : (i == 0 ? laneStep : -laneStep);
                float lateralOffset = tangentSpawnSpacing * laneIndex;
                Vector2 candidatePosition = contactPoint + outwardDirection * dockOffsetFromBase + tangentDirection * lateralOffset;
                fallbackPosition = candidatePosition;

                if (IsDockPositionFree(candidatePosition))
                    return new Vector3(candidatePosition.x, candidatePosition.y, baseTransform.position.z);
            }
        }

        return new Vector3(fallbackPosition.x, fallbackPosition.y, baseTransform.position.z);
    }

    private bool IsDockPositionFree(Vector2 candidatePosition)
    {
        AlliedDefender[] defenders = FindObjectsOfType<AlliedDefender>();
        for (int i = 0; i < defenders.Length; i++)
        {
            AlliedDefender defender = defenders[i];
            if (defender == null)
                continue;

            if (Vector2.Distance(candidatePosition, defender.transform.position) < minimumDockSeparation)
                return false;
        }

        return true;
    }
}
