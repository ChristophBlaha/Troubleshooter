using UnityEngine;

/// <summary>
/// Verbündete Einheit, die die Base verteidigt
/// Spawnt neben der Base und schießt auf Gegner, die der Base am nächsten sind
/// Bewegt sich zum Gegner hin und schaut in Schussrichtung
/// </summary>
public class AlliedDefender : MonoBehaviour
{
    [SerializeField] private float shootCooldown = 1.5f;
    [SerializeField] private int damagePerShot = 2;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Vector3 defaultVisualRotation = new Vector3(0f, 0f, -90f);
    [SerializeField] private float moveSpeed = 2.5f;  // Reduziert für sanftere Bewegung
    [SerializeField] private float preferredDistance = 5f;  // Abstand zum Gegner halten
    [SerializeField] private float viewportMargin = 1f;      // Abstand vom Bildschirmrand
    [SerializeField] private float movementDamping = 0.1f;   // Sanfte Bewegungs-Übergänge

    private Transform baseTarget;
    private float shootTimer = 0f;
    private Rigidbody2D rb;
    private Camera mainCamera;
    private Vector2 targetVelocity = Vector2.zero;  // Für sanfte Bewegungs-Übergänge
    private float formationOffset = 0f;  // Position im Kreis um Gegner
    private bool hasLeftBase = false;  // Flag: ist der Defender bereits aus der Base heraus?

    private void Start()
    {
        baseTarget = GameObject.FindGameObjectWithTag("Base")?.transform;
        transform.rotation = Quaternion.Euler(defaultVisualRotation);
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        
        // Alle Defenders bekommen unterschiedliche Formation-Position
        AlliedDefender[] allDefenders = FindObjectsOfType<AlliedDefender>();
        for (int i = 0; i < allDefenders.Length; i++)
        {
            if (allDefenders[i] == this)
            {
                formationOffset = i * (360f / allDefenders.Length);
                break;
            }
        }
    }

    private void Update()
    {
        if (baseTarget == null)
        {
            baseTarget = GameObject.FindGameObjectWithTag("Base")?.transform;
        }

        // Im Bildschirm bleiben
        ClampToViewport();

        // Wenn Defender die Base verlässt, setze Flag
        if (!hasLeftBase && baseTarget != null && Vector2.Distance(transform.position, baseTarget.position) > 3f)
        {
            hasLeftBase = true;
        }

        // Nur zerstöre wenn er bereits herauskam und zurückkommt
        if (hasLeftBase && baseTarget != null && Vector2.Distance(transform.position, baseTarget.position) < 0.5f)
        {
            Destroy(gameObject);
            return;
        }

        shootTimer += Time.deltaTime;

        // Gegner suchen, der der Base am nächsten ist
        GazeDamageable targetEnemy = FindClosestEnemyToBase();
        
        // Debug: Schießen aktivieren auch wenn Gegner nicht im Bildschirm ist
        if (targetEnemy != null && shootTimer >= shootCooldown)
        {
            ShootAt(targetEnemy.gameObject.transform);
            shootTimer = 0f;
        }

        // Bewegung: zum Gegner oder zur Base, aber Abstand halten
        MoveTowardTarget(targetEnemy);
    }

    private GazeDamageable FindClosestEnemyToBase()
    {
        if (baseTarget == null)
            return null;

        GazeDamageable[] allEnemies = FindObjectsOfType<GazeDamageable>();
        GazeDamageable closestToBase = null;
        float closestDist = float.MaxValue;

        foreach (var enemy in allEnemies)
        {
            if (enemy == null)
                continue;

            // Nur echte Gegner schießen: keine FriendReturningHome-Komponente
            if (enemy.GetComponent<FriendReturningHome>() != null)
                continue;

            float distToBase = Vector2.Distance(baseTarget.position, enemy.transform.position);
            if (distToBase < closestDist)
            {
                closestToBase = enemy;
                closestDist = distToBase;
            }
        }

        return closestToBase;
    }

    private void MoveTowardTarget(GazeDamageable target)
    {
        Vector2 moveDirection = Vector2.zero;

        if (target != null)
        {
            // Formation: Positioniere Defenders im Kreis um den Gegner
            float formationAngle = formationOffset * Mathf.Deg2Rad;
            float formationRadius = 3.5f;  // Radius um Gegner herum
            Vector2 formationPosition = (Vector2)target.transform.position + 
                                       new Vector2(Mathf.Cos(formationAngle), Mathf.Sin(formationAngle)) * formationRadius;
            
            Vector2 toFormationPos = (formationPosition - (Vector2)transform.position);
            float distToFormation = toFormationPos.magnitude;
            
            // Bewegungslogik: Zur Formation-Position gehen
            if (distToFormation > 0.5f)
            {
                moveDirection = toFormationPos.normalized;
            }
            else
            {
                // In Position: stehen bleiben
                moveDirection = Vector2.zero;
            }
        }
        else if (baseTarget != null)
        {
            moveDirection = (baseTarget.position - transform.position).normalized;
        }

        if (rb != null)
        {
            // Sanfte Bewegungs-Interpolation statt sofortige Änderung
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, moveDirection * moveSpeed, movementDamping);
        }
    }

    private bool IsTargetOnScreen(Transform target)
    {
        if (mainCamera == null)
            return true;

        Vector3 viewportPos = mainCamera.WorldToViewportPoint(target.position);
        return viewportPos.x > -viewportMargin && viewportPos.x < 1 + viewportMargin &&
               viewportPos.y > -viewportMargin && viewportPos.y < 1 + viewportMargin &&
               viewportPos.z > 0;
    }

    private void ClampToViewport()
    {
        if (mainCamera == null)
            return;

        Vector3 viewportPos = mainCamera.WorldToViewportPoint(transform.position);
        
        // Clampe Position im Bildschirm mit Margin
        viewportPos.x = Mathf.Clamp01(viewportPos.x);
        viewportPos.y = Mathf.Clamp01(viewportPos.y);
        viewportPos.z = mainCamera.nearClipPlane + 1f;

        Vector3 clampedWorldPos = mainCamera.ViewportToWorldPoint(viewportPos);
        transform.position = new Vector3(clampedWorldPos.x, clampedWorldPos.y, transform.position.z);

        // Stoppe Velocity wenn an Rand
        if (rb != null && (viewportPos.x == 0 || viewportPos.x == 1 || viewportPos.y == 0 || viewportPos.y == 1))
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void FaceDirection(Vector2 direction)
    {
        if (direction.magnitude < 0.01f)
            return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
    }

    private void ShootAt(Transform target)
    {
        Vector2 direction = (target.position - transform.position).normalized;
        FaceDirection(direction);

        if (projectilePrefab == null)
        {
            Debug.LogWarning("AlliedDefender: ProjectilePrefab nicht gesetzt!");
            return;
        }

        GameObject projectile = Instantiate(
            projectilePrefab,
            transform.position,
            Quaternion.Euler(defaultVisualRotation)
        );

        Rigidbody2D projectileRb = projectile.GetComponent<Rigidbody2D>();
        if (projectileRb != null)
        {
            projectileRb.linearVelocity = direction * projectileSpeed;
        }

        // Projectile mit Damage
        AlliedProjectile proj = projectile.GetComponent<AlliedProjectile>();
        if (proj != null)
        {
            proj.damage = damagePerShot;
        }

        if (AudioManager.Instance)
        {
            AudioManager.Instance.PlaySFX("allied_shoot", 0.6f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 8f);
    }
}
 
