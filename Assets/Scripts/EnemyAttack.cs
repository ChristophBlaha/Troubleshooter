using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAttack : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float hitCooldown = 0.2f;
    [SerializeField] private float bounceForce = 5f;

    private float lastHitTime;
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

        // Damage
        if (Time.time >= lastHitTime + hitCooldown)
        {
            lastHitTime = Time.time;

            BaseHealth baseHealth = collision.gameObject.GetComponent<BaseHealth>();
            if (baseHealth != null)
            {
                baseHealth.TakeDamage(damage);
            }
        }

        // 🔥 Bounce Richtung
        Vector2 bounceDir = (transform.position - collision.transform.position).normalized;

        // Velocity reset + Impuls
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(bounceDir * bounceForce, ForceMode2D.Impulse);

        // ❗ Movement kurz pausieren
        if (movement != null)
        {
            movement.TriggerBounce(0.2f);
        }
    }
}