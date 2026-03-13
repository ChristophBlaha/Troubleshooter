using UnityEngine;
using UnityEngine.UI;

public class GazeDamageable : GazeInteractable
{
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private int damagePerTick = 1;
    [SerializeField] private float damageInterval = 0.2f;
    [SerializeField] private Slider healthBar;

    private int currentHealth;
    private float timer;
    private bool isGazing;

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
    }

    protected override void OnGazeExitCallback()
    {
        isGazing = false;
        timer = 0f;
    }

    private void TakeDamage(int damage)
    {
        currentHealth -= damage;

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