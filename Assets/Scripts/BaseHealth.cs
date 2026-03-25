using UnityEngine;
using UnityEngine.UI;

public class BaseHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 50;
    [SerializeField] private Slider healthBar;
    [SerializeField] private GameObject gameOverScreen;
    

    private int currentHealth;

    private void Start()
    {
        if(gameOverScreen != null)
            gameOverScreen.SetActive(false);
        currentHealth = maxHealth;
        UpdateHealthBar();
    }

    public void TakeDamage(int damage)
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
        {
            healthBar.value = (float)currentHealth / maxHealth;
        }
        Debug.Log(healthBar.value);
    }

    private void Die()
    {
        Debug.Log("Base destroyed!");
        // z. B. Game Over
        if(gameOverScreen != null)
            gameOverScreen.SetActive(true);
    }
}