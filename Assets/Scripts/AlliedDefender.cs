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
    [SerializeField] private Color rescuedTint = new Color(0.2f, 1f, 0.38f, 1f);
    [SerializeField] private float returnPortalVolume = 0.8f;
    [SerializeField] private float returnPortalPitch = 1.22f;
    [SerializeField] private float launchImpulse = 1.25f;
    [SerializeField] private float absorbStartDistance = 0.7f;
    [SerializeField] private float absorbDuration = 0.35f;
    [SerializeField] private float armingDuration = 0.35f;
    [SerializeField] private float undockDuration = 0.45f;
    [SerializeField] private int dockedSortingOrder = 18;
    [SerializeField] private int absorbSortingOrder = 20;

    private Transform baseTarget;
    private float shootTimer = 0f;
    private Rigidbody2D rb;
    private Camera mainCamera;
    private Vector2 targetVelocity = Vector2.zero;  // Für sanfte Bewegungs-Übergänge
    private float formationOffset = 0f;  // Position im Kreis um Gegner
    private bool hasLeftBase = false;  // Flag: ist der Defender bereits aus der Base heraus?
    private SpriteRenderer spriteRenderer;
    private bool isAbsorbingIntoBase;
    private float absorbTimer;
    private Vector3 absorbStartPosition;
    private Vector3 absorbStartScale;
    private Collider2D[] cachedColliders;
    private bool isArmingAtBase;
    private float armTimer;
    private float undockTimer;
    private Vector3 dockPosition;
    private Vector2 launchDirection;
    private int originalSortingOrder;
    private bool isUndockingFromBase;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        cachedColliders = GetComponentsInChildren<Collider2D>(true);
        if (spriteRenderer != null)
            originalSortingOrder = spriteRenderer.sortingOrder;
    }

    private void Start()
    {
        baseTarget = GameObject.FindGameObjectWithTag("Base")?.transform;
        transform.rotation = Quaternion.Euler(defaultVisualRotation);
        mainCamera = Camera.main;
        ApplyRescueTint();

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

        if (isAbsorbingIntoBase)
        {
            UpdateBaseAbsorption();
            return;
        }

        if (isArmingAtBase)
        {
            UpdateArmingSequence();
            return;
        }

        // Im Bildschirm bleiben
        ClampToViewport();

        // Wenn Defender die Base verlässt, setze Flag
        if (!hasLeftBase && baseTarget != null && Vector2.Distance(transform.position, baseTarget.position) > 3f)
        {
            hasLeftBase = true;
        }

        // Nur zerstöre wenn er bereits herauskam und zurückkommt
        if (hasLeftBase && baseTarget != null && Vector2.Distance(transform.position, baseTarget.position) < absorbStartDistance)
        {
            BeginBaseAbsorption();
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

    public void InitializeFromRescue(Transform rescuedBase, Vector2 launchDirection)
    {
        baseTarget = rescuedBase;
        ApplyRescueTint();
        hasLeftBase = false;
        isAbsorbingIntoBase = false;
        isArmingAtBase = true;
        isUndockingFromBase = false;
        armTimer = armingDuration;
        undockTimer = undockDuration;
        this.launchDirection = launchDirection.sqrMagnitude > 0.001f ? launchDirection.normalized : Vector2.up;
        dockPosition = transform.position;
        transform.localScale = Vector3.one * 0.5f;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = true;
        }

        SetSpriteSortingOrder(dockedSortingOrder);
    }

    private void ApplyRescueTint()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = rescuedTint;
    }

    private void BeginBaseAbsorption()
    {
        if (isAbsorbingIntoBase)
            return;

        isAbsorbingIntoBase = true;
        isArmingAtBase = false;
        isUndockingFromBase = false;
        absorbTimer = 0f;
        absorbStartPosition = transform.position;
        absorbStartScale = transform.localScale;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        if (cachedColliders != null)
        {
            for (int i = 0; i < cachedColliders.Length; i++)
            {
                if (cachedColliders[i] != null)
                    cachedColliders[i].enabled = false;
            }
        }

        if (AudioManager.Instance)
            AudioManager.Instance.PlaySFX("ally_returned_home", returnPortalVolume, returnPortalPitch);

        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = absorbSortingOrder;
    }

    private void UpdateBaseAbsorption()
    {
        if (baseTarget == null)
        {
            Destroy(gameObject);
            return;
        }

        absorbTimer += Time.deltaTime;
        float t = Mathf.Clamp01(absorbTimer / absorbDuration);
        float eased = 1f - Mathf.Pow(1f - t, 3f);

        transform.position = Vector3.Lerp(absorbStartPosition, baseTarget.position, eased);
        transform.localScale = Vector3.Lerp(absorbStartScale, Vector3.zero, eased);

        if (t >= 1f)
            Destroy(gameObject);
    }

    private void UpdateArmingSequence()
    {
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        transform.localScale = Vector3.one * 0.5f;

        if (armTimer > 0f)
        {
            armTimer -= Time.deltaTime;
            transform.position = dockPosition;
            return;
        }

        if (!isUndockingFromBase)
        {
            isUndockingFromBase = true;
            SetSpriteSortingOrder(dockedSortingOrder);
        }

        if (undockTimer > 0f)
        {
            undockTimer -= Time.deltaTime;
            transform.position += (Vector3)(launchDirection * launchImpulse * Time.deltaTime);
            return;
        }

        isArmingAtBase = false;
        isUndockingFromBase = false;
        hasLeftBase = true;
        SetSpriteSortingOrder(originalSortingOrder);

        if (rb != null)
            rb.linearVelocity = launchDirection * launchImpulse;
    }

    private void SetSpriteSortingOrder(int sortingOrder)
    {
        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = sortingOrder;
    }
}
 
