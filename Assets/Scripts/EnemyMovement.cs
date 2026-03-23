using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMovement : MonoBehaviour
{
    public Transform baseTarget;
    [SerializeField] private float speed = 5f;

    private Rigidbody2D rb;
    private float bounceTimer = 0f;
    private float bounceDuration = 0.2f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (baseTarget == null) return;

        // ❗ während Bounce NICHT bewegen
        if (bounceTimer > 0f)
        {
            bounceTimer -= Time.fixedDeltaTime;
            return;
        }

        Vector2 direction = (baseTarget.position - transform.position).normalized;

        rb.linearVelocity = direction * speed;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle + 90f);
    }

    public void TriggerBounce(float duration)
    {
        bounceTimer = duration;
    }
}