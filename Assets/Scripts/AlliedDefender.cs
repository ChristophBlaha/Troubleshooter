using UnityEngine;

/// <summary>
/// Verbündete Einheit, die die Base verteidigt.
/// Sie bewaffnet sich an der Base, verteidigt die Umgebung und kehrt nach Wave-Ende zurück.
/// </summary>
public class AlliedDefender : MonoBehaviour
{
    [SerializeField] private float shootCooldown = 1.35f;
    [SerializeField] private int damagePerShot = 2;
    [SerializeField] private float projectileSpeed = 6.2f;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Vector3 defaultVisualRotation = new Vector3(0f, 0f, -90f);
    [SerializeField] private float moveSpeed = 2.35f;
    [SerializeField] private float viewportMargin = 1f;
    [SerializeField] private float movementDamping = 0.12f;
    [SerializeField] private float aimVarianceDegrees = 8f;
    [SerializeField] private Color rescuedTint = new Color(0.2f, 1f, 0.38f, 1f);
    [SerializeField] private float returnPortalVolume = 0.8f;
    [SerializeField] private float returnPortalPitch = 1.22f;
    [SerializeField] private float launchImpulse = 1.3f;
    [SerializeField] private float absorbStartDistance = 0.7f;
    [SerializeField] private float waveEndAbsorbStartDistance = 1.35f;
    [SerializeField] private float absorbDuration = 0.35f;
    [SerializeField] private float armingDuration = 0.35f;
    [SerializeField] private float undockDuration = 0.45f;
    [SerializeField] private int dockedSortingOrder = 18;
    [SerializeField] private int absorbSortingOrder = 20;
    [SerializeField] private int maxHealth = 6;
    [SerializeField] private float idleGuardRadius = 2.45f;
    [SerializeField] private float idleGuardTolerance = 0.4f;
    [SerializeField] private float maxChaseDistanceFromBase = 5.4f;
    [SerializeField] private float hardLeashDistanceFromBase = 6.15f;
    [SerializeField] private Vector2 combatColliderSize = new Vector2(1.6f, 1.6f);
    [SerializeField] private float allySeparationRadius = 1.3f;
    [SerializeField] private float allySeparationStrength = 1.45f;
    [SerializeField] private float targetLaneOffset = 0.95f;
    [SerializeField] private float formationRadius = 2.35f;
    [SerializeField] private float formationRadiusVariance = 0.45f;
    [SerializeField] private float laneWaveAmplitude = 0.45f;
    [SerializeField] private float laneWaveSpeed = 0.9f;

    private Transform baseTarget;
    private float shootTimer;
    private Rigidbody2D rb;
    private Camera mainCamera;
    private float formationOffset;
    private bool hasLeftBase;
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
    private bool returnAfterWaveEnd;
    private GazeDamageable friendlyDamageable;
    private BoxCollider2D boxCollider;
    private WaveManager waveManager;
    private float guardRadiusOffset;
    private float personalLaneOffset;
    private float personalFormationRadius;
    private float movementSeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
        cachedColliders = GetComponentsInChildren<Collider2D>(true);

        if (spriteRenderer != null)
            originalSortingOrder = spriteRenderer.sortingOrder;

        InitializeMovementIdentity();

        EnsureFriendlyDamageable();
        EnsureCombatCollider();
    }

    private void Start()
    {
        baseTarget = GameObject.FindGameObjectWithTag("Base")?.transform;
        transform.rotation = Quaternion.Euler(defaultVisualRotation);
        mainCamera = Camera.main;
        waveManager = WaveManager.Instance;
        ApplyRescueTint();

        if (waveManager != null)
            waveManager.OnWaveComplete.AddListener(HandleWaveComplete);

    }

    private void OnDestroy()
    {
        if (waveManager != null)
            waveManager.OnWaveComplete.RemoveListener(HandleWaveComplete);
    }

    private void Update()
    {
        if (baseTarget == null)
            baseTarget = GameObject.FindGameObjectWithTag("Base")?.transform;

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

        ClampToViewport();

        if (!hasLeftBase && baseTarget != null && Vector2.Distance(transform.position, baseTarget.position) > 2.8f)
            hasLeftBase = true;

        shootTimer += Time.deltaTime;

        bool forcedReturnToBase = returnAfterWaveEnd && hasLeftBase;
        GazeDamageable targetEnemy = forcedReturnToBase ? null : FindClosestEnemyToBase();
        bool shouldReturnToBase = hasLeftBase && (forcedReturnToBase || (targetEnemy == null && IsWaveCooldownActive()));

        float absorbThreshold = forcedReturnToBase ? waveEndAbsorbStartDistance : absorbStartDistance;
        if (shouldReturnToBase && baseTarget != null && Vector2.Distance(transform.position, baseTarget.position) < absorbThreshold)
        {
            BeginBaseAbsorption();
            return;
        }

        if (targetEnemy != null && shootTimer >= shootCooldown)
        {
            ShootAt(targetEnemy.transform);
            shootTimer = 0f;
        }

        MoveTowardTarget(targetEnemy, shouldReturnToBase);
    }

    private GazeDamageable FindClosestEnemyToBase()
    {
        if (baseTarget == null)
            return null;

        GazeDamageable[] allEnemies = FindObjectsOfType<GazeDamageable>();
        GazeDamageable closestToBase = null;
        float closestDist = float.MaxValue;

        for (int i = 0; i < allEnemies.Length; i++)
        {
            GazeDamageable enemy = allEnemies[i];
            if (enemy == null || enemy == friendlyDamageable || enemy.Team != DamageableTeam.Hostile)
                continue;

            if (enemy.GetComponent<FriendReturningHome>() != null)
                continue;

            float distToBase = Vector2.Distance(baseTarget.position, enemy.transform.position);
            if (distToBase > maxChaseDistanceFromBase)
                continue;

            if (distToBase < closestDist)
            {
                closestToBase = enemy;
                closestDist = distToBase;
            }
        }

        return closestToBase;
    }

    private void MoveTowardTarget(GazeDamageable target, bool shouldReturnToBase)
    {
        Vector2 moveDirection = Vector2.zero;

        if (baseTarget != null)
        {
            float distanceToBase = Vector2.Distance(transform.position, baseTarget.position);
            if (!shouldReturnToBase && distanceToBase > hardLeashDistanceFromBase)
            {
                moveDirection = ((Vector2)baseTarget.position - (Vector2)transform.position).normalized;
                ApplyVelocity(moveDirection);
                return;
            }
        }

        if (target != null)
        {
            Vector2 toEnemy = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
            if (toEnemy.sqrMagnitude < 0.001f)
                toEnemy = Vector2.up;

            Vector2 baseToEnemy = baseTarget != null
                ? ((Vector2)target.transform.position - (Vector2)baseTarget.position).normalized
                : toEnemy;
            if (baseToEnemy.sqrMagnitude < 0.001f)
                baseToEnemy = toEnemy;

            Vector2 perpendicular = new Vector2(-baseToEnemy.y, baseToEnemy.x);
            float laneWaveOffset = Mathf.Sin((Time.time * laneWaveSpeed) + movementSeed) * laneWaveAmplitude;
            Vector2 formationPosition = (Vector2)target.transform.position
                - baseToEnemy * personalFormationRadius
                + perpendicular * (personalLaneOffset + laneWaveOffset);

            if (baseTarget != null)
            {
                Vector2 fromBase = formationPosition - (Vector2)baseTarget.position;
                if (fromBase.magnitude > maxChaseDistanceFromBase)
                    formationPosition = (Vector2)baseTarget.position + fromBase.normalized * maxChaseDistanceFromBase;
            }

            Vector2 toFormationPos = formationPosition - (Vector2)transform.position;
            if (toFormationPos.magnitude > 0.35f)
                moveDirection = toFormationPos.normalized;
        }
        else if (baseTarget != null)
        {
            moveDirection = shouldReturnToBase
                ? ((Vector2)baseTarget.position - (Vector2)transform.position).normalized
                : GetGuardDirection();
        }

        moveDirection = ApplyAllySeparation(moveDirection, shouldReturnToBase);
        ApplyVelocity(moveDirection);
    }

    private void ApplyVelocity(Vector2 moveDirection)
    {
        if (rb == null)
            return;

        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, moveDirection * moveSpeed, movementDamping);
    }

    private void ClampToViewport()
    {
        if (mainCamera == null)
            return;

        Vector3 viewportPos = mainCamera.WorldToViewportPoint(transform.position);
        viewportPos.x = Mathf.Clamp01(viewportPos.x);
        viewportPos.y = Mathf.Clamp01(viewportPos.y);
        viewportPos.z = mainCamera.nearClipPlane + 1f;

        Vector3 clampedWorldPos = mainCamera.ViewportToWorldPoint(viewportPos);
        transform.position = new Vector3(clampedWorldPos.x, clampedWorldPos.y, transform.position.z);

        if (rb != null && (viewportPos.x == 0f || viewportPos.x == 1f || viewportPos.y == 0f || viewportPos.y == 1f))
            rb.linearVelocity = Vector2.zero;
    }

    private void FaceDirection(Vector2 direction)
    {
        if (direction.magnitude < 0.01f)
            return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
    }

    private void ShootAt(Transform target)
    {
        Vector2 direction = ((Vector2)target.position - (Vector2)transform.position).normalized;
        direction = ApplyAimVariance(direction);
        FaceDirection(direction);

        if (projectilePrefab == null)
        {
            Debug.LogWarning("AlliedDefender: ProjectilePrefab nicht gesetzt!");
            return;
        }

        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.Euler(defaultVisualRotation));
        Rigidbody2D projectileRb = projectile.GetComponent<Rigidbody2D>();
        if (projectileRb != null)
            projectileRb.linearVelocity = direction * projectileSpeed;

        AlliedProjectile proj = projectile.GetComponent<AlliedProjectile>();
        if (proj != null)
            proj.damage = damagePerShot;

        if (AudioManager.Instance)
            AudioManager.Instance.PlaySFX("allied_shoot", 0.6f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, maxChaseDistanceFromBase);
    }

    public void InitializeFromRescue(Transform rescuedBase, Vector2 rescuedLaunchDirection)
    {
        baseTarget = rescuedBase;
        ApplyRescueTint();
        hasLeftBase = false;
        returnAfterWaveEnd = false;
        isAbsorbingIntoBase = false;
        isArmingAtBase = true;
        isUndockingFromBase = false;
        armTimer = armingDuration;
        undockTimer = undockDuration;
        launchDirection = rescuedLaunchDirection.sqrMagnitude > 0.001f ? rescuedLaunchDirection.normalized : Vector2.up;
        dockPosition = transform.position;
        transform.localScale = Vector3.one * 0.5f;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = true;
        }

        SetSpriteSortingOrder(dockedSortingOrder);
    }

    private void EnsureFriendlyDamageable()
    {
        friendlyDamageable = GetComponent<GazeDamageable>();
        if (friendlyDamageable == null)
            friendlyDamageable = gameObject.AddComponent<GazeDamageable>();

        friendlyDamageable.ConfigureRuntime(DamageableTeam.Friendly, maxHealth, false, true);
    }

    private void EnsureCombatCollider()
    {
        if (boxCollider == null)
            return;

        boxCollider.size = combatColliderSize;
        boxCollider.offset = Vector2.zero;
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

        SetSpriteSortingOrder(absorbSortingOrder);
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
        {
            if (Score.Instance != null)
                Score.Instance.AddFriendlyRescuedScore();

            Destroy(gameObject);
        }
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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryBeginAbsorptionFromBaseContact(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryBeginAbsorptionFromBaseContact(collision);
    }

    private Vector2 ApplyAimVariance(Vector2 direction)
    {
        float angleOffset = Random.Range(-aimVarianceDegrees, aimVarianceDegrees);
        return Quaternion.Euler(0f, 0f, angleOffset) * direction;
    }

    private bool IsWaveCooldownActive()
    {
        if (waveManager == null)
            waveManager = WaveManager.Instance;

        if (waveManager == null)
            return true;

        return !waveManager.IsWaveActive() || waveManager.IsWavePaused();
    }

    private Vector2 GetGuardDirection()
    {
        if (baseTarget == null)
            return Vector2.zero;

        float angle = (Time.time * 0.9f) + formationOffset * Mathf.Deg2Rad;
        Vector2 guardPoint = (Vector2)baseTarget.position +
            new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (idleGuardRadius + guardRadiusOffset);
        Vector2 toGuardPoint = guardPoint - (Vector2)transform.position;

        if (toGuardPoint.magnitude <= idleGuardTolerance)
            return Vector2.zero;

        return toGuardPoint.normalized;
    }

    private void HandleWaveComplete(int waveNumber)
    {
        returnAfterWaveEnd = true;
    }

    private void TryBeginAbsorptionFromBaseContact(Collision2D collision)
    {
        if (collision == null || !collision.gameObject.CompareTag("Base"))
            return;

        if (isArmingAtBase || isAbsorbingIntoBase || !hasLeftBase)
            return;

        if (!returnAfterWaveEnd && !IsWaveCooldownActive())
            return;

        BeginBaseAbsorption();
    }

    private void InitializeMovementIdentity()
    {
        int seed = Mathf.Abs(GetInstanceID());
        formationOffset = Hash01(seed + 11) * 360f;
        guardRadiusOffset = Mathf.Lerp(-0.4f, 0.55f, Hash01(seed + 23));
        personalLaneOffset = Mathf.Lerp(-targetLaneOffset, targetLaneOffset, Hash01(seed + 47));
        personalFormationRadius = formationRadius + Mathf.Lerp(-formationRadiusVariance, formationRadiusVariance, Hash01(seed + 71));
        movementSeed = Hash01(seed + 101) * 10f;
    }

    private Vector2 ApplyAllySeparation(Vector2 moveDirection, bool shouldReturnToBase)
    {
        AlliedDefender[] defenders = FindObjectsOfType<AlliedDefender>();
        Vector2 separation = Vector2.zero;
        int nearbyCount = 0;

        for (int i = 0; i < defenders.Length; i++)
        {
            AlliedDefender other = defenders[i];
            if (other == null || other == this || other.isAbsorbingIntoBase)
                continue;

            Vector2 offset = (Vector2)transform.position - (Vector2)other.transform.position;
            float distance = offset.magnitude;
            if (distance <= 0.001f || distance > allySeparationRadius)
                continue;

            float weight = 1f - (distance / allySeparationRadius);
            separation += offset.normalized * weight;
            nearbyCount++;
        }

        if (nearbyCount == 0)
            return moveDirection;

        separation /= nearbyCount;
        float separationWeight = shouldReturnToBase ? allySeparationStrength * 0.55f : allySeparationStrength;
        Vector2 blended = moveDirection + separation * separationWeight;
        return blended.sqrMagnitude > 0.001f ? blended.normalized : moveDirection;
    }

    private static float Hash01(int seed)
    {
        uint x = (uint)seed;
        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;
        return (x & 0x00FFFFFF) / 16777215f;
    }
}
