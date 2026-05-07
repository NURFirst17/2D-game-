using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Normal Attack")]
    [SerializeField] private int normalAttackDamage = 1;
    [SerializeField] private float normalAttackRange = 0.6f;
    [SerializeField] private float normalAttackCooldown = 0.35f;

    [Header("Attack Settings")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private float attackStateDuration = 0.15f;
    [SerializeField] private KeyCode attackKey = KeyCode.J;

    private float attackCooldownTimer;
    private float attackStateTimer;
    private int _attackHash;
    private bool _hasAttackTrigger;

    public bool IsAttacking { get; private set; }
    public KeyCode AttackKey => attackKey;

    private void Awake()
    {
        _attackHash = Animator.StringToHash("Attack");

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }

        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        _hasAttackTrigger = HasTrigger(animator, "Attack");
    }

    public void Tick(float deltaTime)
    {
        if (playerHealth != null && playerHealth.IsDead)
        {
            IsAttacking = false;
            return;
        }

        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= deltaTime;
        }

        if (attackStateTimer > 0f)
        {
            attackStateTimer -= deltaTime;
            if (attackStateTimer <= 0f)
            {
                ResetAttackState();
            }
        }
    }

    public bool CanStartAttack(bool isGrounded)
    {
        if (attackCooldownTimer > 0f)
            return false;

        if (attackPoint == null)
            return false;

        if (!isGrounded)
            return false;

        return true;
    }

    public bool TryStartAttack()
    {
        if (!CanStartAttack(characterController != null && characterController.IsGrounded()))
        {
            return false;
        }

        IsAttacking = true;
        attackCooldownTimer = normalAttackCooldown;
        attackStateTimer = attackStateDuration;

        if (animator != null)
        {
            if (_hasAttackTrigger)
            {
                animator.SetTrigger(_attackHash);
            }
        }

        DealDamage(normalAttackDamage, normalAttackRange);
        return true;
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
            var damageable = enemy.GetComponent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
            {
                damageable.TakeDamage(damage);
            }
        }
    }

    private void ResetAttackState()
    {
        IsAttacking = false;
        attackStateTimer = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, normalAttackRange);
    }

    private static bool HasTrigger(Animator targetAnimator, string parameterName)
    {
        if (targetAnimator == null)
            return false;

        foreach (var parameter in targetAnimator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == parameterName)
            {
                return true;
            }
        }

        return false;
    }
}
