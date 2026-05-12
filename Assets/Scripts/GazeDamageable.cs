using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider2D))]
public class GazeDamageable : GazeInteractable
{
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private int damagePerTick = 1;
    [SerializeField] private float damageInterval = 0.2f;
    [SerializeField] private Slider healthBar;
    [SerializeField, Range(0.1f, 1f)] private float externalHitboxScale = 0.55f;
    [SerializeField] private float externalHitboxPadding = 0.04f;

    public int currentHealth { get; set; }
    public int MaxHealth 
    { 
        get { return maxHealth; }
        set { maxHealth = value; }
    }

    private float timer;
    private bool isGazing;
    public UnityEvent OnDeath = new UnityEvent();

    private void Awake()
    {
        // Sicherstellen, dass Collider existiert
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null)
        {
            Debug.LogError("BoxCollider2D fehlt auf " + gameObject.name);
        }
    }

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();
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
            healthBar.value = (float)currentHealth / maxHealth;
    }

    private void Die()
    {
        if (AudioManager.Instance)
        {
            AudioManager.Instance.PlaySFX("enemy_death", 0.8f);
        }
        OnDeath?.Invoke();

        // Notify WaveController
        if (WaveController.Instance != null)
        {
            WaveController.Instance.RegisterEnemyDeath();
        }

        Destroy(gameObject);
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
}
