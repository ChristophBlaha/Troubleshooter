using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class AlliedProjectile : MonoBehaviour
{
    public int damage = 2;
    [SerializeField] private float lifetime = 10f;
    [SerializeField] private Vector3 defaultVisualRotation = new Vector3(0f, 0f, -90f);
    [SerializeField] private float visualScaleMultiplier = 0.3f;
    [SerializeField] private float preciseHitPadding = 0.02f;

    private bool hasHitTarget;

    private void Start()
    {
        transform.rotation = Quaternion.Euler(defaultVisualRotation);
        transform.localScale *= visualScaleMultiplier;
        Destroy(gameObject, lifetime);

        // Gerade Bewegung ohne Bogen - Gravity ausschalten
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            // Kinematic für schnelle Collision Detection bei bewegten Gegnern
            rb.isKinematic = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        // Collider als Trigger für zuverlässige Erkennung
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryHitEnemy(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        TryHitEnemy(collision);
    }

    private void TryHitEnemy(Collider2D collision)
    {
        if (hasHitTarget || collision == null)
            return;

        GazeDamageable enemy = collision.GetComponent<GazeDamageable>();
        if (enemy == null)
            return;

        if (enemy.Team != DamageableTeam.Hostile)
            return;

        if (!enemy.IsPreciseProjectileHit(transform.position, preciseHitPadding))
            return;

        hasHitTarget = true;
        enemy.TakeDamageExternal(damage);

        if (AudioManager.Instance)
        {
            AudioManager.Instance.PlaySFX("projectile_hit", 0.6f);
        }

        Destroy(gameObject);
    }
}
