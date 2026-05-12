using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public enum DamageableTeam
{
    Hostile = 0,
    Friendly = 1
}

[RequireComponent(typeof(BoxCollider2D))]
public class GazeDamageable : GazeInteractable
{
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private int damagePerTick = 1;
    [SerializeField] private float damageInterval = 0.2f;
    [SerializeField] private Slider healthBar;
    [SerializeField] private DamageableTeam team = DamageableTeam.Hostile;
    [SerializeField] private bool registerDeathWithWaveController = true;
    [SerializeField] private bool autoCreateHealthBarIfMissing;
    [SerializeField] private Vector3 autoHealthBarOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private Vector2 autoHealthBarSize = new Vector2(1.6f, 0.24f);
    [SerializeField, Range(0.1f, 1f)] private float externalHitboxScale = 0.55f;
    [SerializeField] private float externalHitboxPadding = 0.04f;

    public int currentHealth { get; set; }
    public DamageableTeam Team => team;
    public int MaxHealth 
    { 
        get { return maxHealth; }
        set { maxHealth = value; }
    }

    private float timer;
    private bool isGazing;
    private Collider2D[] unitColliders;
    public UnityEvent OnDeath = new UnityEvent();

    private void Awake()
    {
        // Sicherstellen, dass Collider existiert
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null)
        {
            Debug.LogError("BoxCollider2D fehlt auf " + gameObject.name);
        }

        unitColliders = GetComponentsInChildren<Collider2D>(true);
    }

    private void Start()
    {
        UnitCollisionRegistry.RegisterUnit(unitColliders);
        EnsureHealthBar();
        currentHealth = maxHealth;
        UpdateHealthBar();
    }

    private void OnDestroy()
    {
        UnitCollisionRegistry.UnregisterUnit(unitColliders);
    }

    protected override void Update()
    {
        base.Update();

        if (!isGazing)
            return;

        timer += Time.deltaTime;

        if (timer >= damageInterval)
        {
            timer = 0f;
            TakeDamage(damagePerTick);
        }
    }

    protected override void OnGazeEnterCallback()
    {
        isGazing = true;
        if (AudioManager.Instance)
        {
            AudioManager.Instance.PlaySFX("gaze_hit", 0.7f);
        }
        Debug.Log("Gaze ENTER: " + gameObject.name);
    }

    protected override void OnGazeExitCallback()
    {
        isGazing = false;
        timer = 0f;
        Debug.Log("Gaze EXIT: " + gameObject.name);
    }

    private void TakeDamage(int damage)
    {
        currentHealth -= damage;

        Debug.Log("DAMAGE → " + currentHealth);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }

        UpdateHealthBar();
    }

    public void TakeDamageExternal(int damage)
    {
        TakeDamage(damage);
    }

    public void ConfigureRuntime(DamageableTeam runtimeTeam, int runtimeMaxHealth, bool registerWaveProgress, bool createHealthBarIfMissing)
    {
        team = runtimeTeam;
        maxHealth = runtimeMaxHealth;
        currentHealth = runtimeMaxHealth;
        registerDeathWithWaveController = registerWaveProgress;
        autoCreateHealthBarIfMissing = createHealthBarIfMissing;
        EnsureHealthBar();
        UpdateHealthBar();
    }

    public bool IsPreciseProjectileHit(Vector2 worldPoint, float additionalPadding = 0f)
    {
        if (TryGetCombatBounds(out Bounds combatBounds))
        {
            Vector3 center = combatBounds.center;
            Vector3 extents = combatBounds.extents;
            extents.x = Mathf.Max(0.01f, extents.x * externalHitboxScale + externalHitboxPadding + additionalPadding);
            extents.y = Mathf.Max(0.01f, extents.y * externalHitboxScale + externalHitboxPadding + additionalPadding);

            return Mathf.Abs(worldPoint.x - center.x) <= extents.x &&
                   Mathf.Abs(worldPoint.y - center.y) <= extents.y;
        }

        float fallbackRadius = 0.2f + additionalPadding;
        return Vector2.Distance(worldPoint, transform.position) <= fallbackRadius;
    }

    private void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.maxValue = 1f;
            healthBar.value = (float)currentHealth / maxHealth;
        }
    }

    private void Die()
    {
        if (AudioManager.Instance)
        {
            AudioManager.Instance.PlaySFX("enemy_death", 0.8f);
        }

        AwardScoreOnDeath();
        OnDeath?.Invoke();

        // Notify WaveController
        if (registerDeathWithWaveController && WaveController.Instance != null)
        {
            WaveController.Instance.RegisterEnemyDeath();
        }

        Destroy(gameObject);
    }

    private void AwardScoreOnDeath()
    {
        if (Score.Instance == null)
            return;

        bool isRescueFriendly = team == DamageableTeam.Friendly || GetComponent<FriendReturningHome>() != null;
        if (isRescueFriendly)
        {
            Score.Instance.AddFriendlyKilledPenalty();
            return;
        }

        Score.Instance.AddEnemyKillScore();
    }

    private bool TryGetCombatBounds(out Bounds bounds)
    {
        SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            bounds = spriteRenderer.bounds;
            return true;
        }

        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            bounds = collider.bounds;
            return true;
        }

        bounds = default;
        return false;
    }

    private void EnsureHealthBar()
    {
        if (healthBar != null || !autoCreateHealthBarIfMissing)
            return;

        GameObject canvasObject = new GameObject("RuntimeHealthBarCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        canvasObject.transform.localPosition = autoHealthBarOffset;
        canvasObject.layer = gameObject.layer;

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 30;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 16f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(100f, 20f);
        canvasRect.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        GameObject sliderObject = new GameObject("HealthBar", typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(canvasObject.transform, false);

        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(autoHealthBarSize.x * 100f, autoHealthBarSize.y * 100f);

        GameObject backgroundObject = CreateHealthBarImage("Background", sliderObject.transform, new Color(0.08f, 0.12f, 0.16f, 0.92f));
        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        GameObject fillAreaObject = new GameObject("Fill Area", typeof(RectTransform));
        fillAreaObject.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillAreaObject.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(3f, 3f);
        fillAreaRect.offsetMax = new Vector2(-3f, -3f);

        GameObject fillObject = CreateHealthBarImage("Fill", fillAreaObject.transform, team == DamageableTeam.Friendly
            ? new Color(0.2f, 1f, 0.38f, 1f)
            : new Color(1f, 0.23f, 0.23f, 1f));
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        healthBar = sliderObject.GetComponent<Slider>();
        healthBar.transition = Selectable.Transition.None;
        healthBar.direction = Slider.Direction.LeftToRight;
        healthBar.minValue = 0f;
        healthBar.maxValue = 1f;
        healthBar.fillRect = fillRect;
        healthBar.targetGraphic = fillObject.GetComponent<Image>();
        healthBar.handleRect = null;
    }

    private GameObject CreateHealthBarImage(string name, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.type = Image.Type.Simple;
        return imageObject;
    }
}
