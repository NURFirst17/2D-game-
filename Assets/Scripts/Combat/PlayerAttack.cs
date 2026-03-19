using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float attackRange = 0.6f;
    [SerializeField] private float attackCooldown = 0.4f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Transform attackPoint;

    private float attackCooldownTimer;
    private Animator animator;
    private PlayerHealth playerHealth;
    private PlayerMovement playerMovement;

    public bool IsAttacking { get; private set; }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        playerHealth = GetComponent<PlayerHealth>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (playerHealth != null && playerHealth.IsDead)
            return;

        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.Mouse0) && CanAttack())
        {
            Attack();
        }
    }

    private bool CanAttack()
    {
        if (attackCooldownTimer > 0f)
            return false;

        if (playerMovement != null && playerMovement.IsDashing)
            return false;

        return true;
    }

    private void Attack()
    {
        IsAttacking = true;
        attackCooldownTimer = attackCooldown;

        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(
            attackPoint.position,
            attackRange,
            enemyLayer
        );

        foreach (Collider2D enemy in hitEnemies)
        {
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();

            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(attackDamage);
            }
        }

        Invoke(nameof(ResetAttackState), 0.15f);
    }

    private void ResetAttackState()
    {
        IsAttacking = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}