using UnityEngine;

public class EnemyChase : MonoBehaviour
{
    [Header("Настройки цели")]
    public Transform player;

    [Header("Настройки движения")]
    public float moveSpeed = 2f;
    public float chaseDistance = 6f;
    public float stopDistance = 1f;

    [Header("Флип спрайта")]
    public bool faceRight = true;

    private Rigidbody2D rb;
    private Vector2 movement;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (player == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= chaseDistance && distanceToPlayer > stopDistance)
        {
            Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;
            movement = new Vector2(direction.x, 0f);

            FlipToTarget();
        }
        else
        {
            movement = Vector2.zero;
        }
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(movement.x * moveSpeed, rb.linearVelocity.y);
    }

    private void FlipToTarget()
    {
        if (player == null) return;

        if (player.position.x > transform.position.x && !faceRight)
        {
            Flip();
        }
        else if (player.position.x < transform.position.x && faceRight)
        {
            Flip();
        }
    }

    private void Flip()
    {
        faceRight = !faceRight;

        Vector3 localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
    }
}