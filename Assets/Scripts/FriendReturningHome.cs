using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FriendReturningHome : MonoBehaviour
{
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

        Destroy(this.gameObject);
        //Dinge die passieren wenn ein Friendly Character die Base erreicht
        Score.Instance.IncreaseScore(10);
    }
}