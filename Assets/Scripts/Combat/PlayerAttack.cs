using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Normal Attack")]
    [SerializeField] private int normalAttackDamage = 1;
    [SerializeField] private float normalAttackRange = 0.6f;
    [SerializeField] private float normalAttackCooldown = 0.35f;

    [Header("Light Attack")]
    [SerializeField] private int lightAttackDamage = 3;
    [SerializeField] private float lightAttackRange = 0.8f;
    [SerializeField] private float lightAttackCooldown = 0.6f;
    [SerializeField] private float lightAttackCost = 20f;

    [Header("Attack Settings")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Transform attackPoint;

    private float attackCooldownTimer;

    private Animator animator;
    private PlayerHealth playerHealth;
    private PlayerMovement playerMovement;
    private PlayerLight playerLight;

    public bool IsAttacking { get; private set; }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        playerHealth = GetComponent<PlayerHealth>();
        playerMovement = GetComponent<PlayerMovement>();
        playerLight = GetComponent<PlayerLight>();
    }

    private void Update()
    {
        if (playerHealth != null && playerHealth.IsDead)
            return;

        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= Time.deltaTime;
        }

        if (Input.GetMouseButtonDown(0) && CanAttack())
        {
            NormalAttack();
        }

        if (Input.GetMouseButtonDown(1) && CanAttack())
        {
            LightAttack();
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

    private void NormalAttack()
    {
        IsAttacking = true;
        attackCooldownTimer = normalAttackCooldown;

        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        DealDamage(normalAttackDamage, normalAttackRange);

        Invoke(nameof(ResetAttackState), 0.15f);
    }

    private void LightAttack()
    {
        if (playerLight != null && !playerLight.TryUseLight(lightAttackCost))
            return;

        IsAttacking = true;
        attackCooldownTimer = lightAttackCooldown;

        if (animator != null)
        {
            animator.SetTrigger("LightAttack");
        }

        DealDamage(lightAttackDamage, lightAttackRange);

        Invoke(nameof(ResetAttackState), 0.2f);
    }

    private void DealDamage(int damage, float range)
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(
            attackPoint.position,
            range,
            enemyLayer
        );

        foreach (Collider2D enemy in hitEnemies)
        {
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();

            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
            }
        }
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
        Gizmos.DrawWireSphere(attackPoint.position, normalAttackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(attackPoint.position, lightAttackRange);
    }
}