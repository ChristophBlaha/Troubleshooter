using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(BoxCollider2D))]
public class GazeDamageable : GazeInteractable
{
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private int damagePerTick = 1;
    [SerializeField] private float damageInterval = 0.2f;
    [SerializeField] private Slider healthBar;

    private int currentHealth;
    private float timer;
    private bool isGazing;

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

    private void UpdateHealthBar()
    {
        if (healthBar != null)
            healthBar.value = (float)currentHealth / maxHealth;
    }

    private void Die()
    {
        Destroy(gameObject);
    }
}