using UnityEngine;

public class PlayerHealth : EntityHealth
{
    public event System.Action Damaged;
    public event System.Action Died;

    [Header("Invincibility")]
    [SerializeField] private float invincibilityDuration = 1f;

    [Header("Knockback")]
    [SerializeField] private float knockbackForceX = 5f;
    [SerializeField] private float knockbackForceY = 4f;

    [Header("Feedback")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private DamageFlashFeedback damageFeedback;

    private float _invincibilityTimer;
    private bool _hasHurtTrigger;
    private bool _hasDeathTrigger;

    public int CurrentHealthValue => CurrentHealth;

    protected override void Awake()
    {
        base.Awake();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (damageFeedback == null)
        {
            damageFeedback = GetComponentInChildren<DamageFlashFeedback>();
        }

        if (damageFeedback == null)
        {
            var spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                damageFeedback = spriteRenderer.GetComponent<DamageFlashFeedback>();
                if (damageFeedback == null)
                {
                    damageFeedback = spriteRenderer.gameObject.AddComponent<DamageFlashFeedback>();
                }
            }
        }

        _hasHurtTrigger = HasTrigger(animator, "Hurt");
        _hasDeathTrigger = HasTrigger(animator, "Death");
    }

    private void Update()
    {
        if (_invincibilityTimer > 0f)
        {
            _invincibilityTimer -= Time.deltaTime;
        }
    }

    public override void TakeDamage(int damage)
    {
        if (IsDead || _invincibilityTimer > 0f)
        {
            return;
        }

        base.TakeDamage(damage);

        if (!IsDead)
        {
            _invincibilityTimer = invincibilityDuration;
        }
    }

    public void Heal(int amount)
    {
        RestoreHealth(amount);
    }

    protected override void OnDamageTaken()
    {
        ApplyKnockback();
        Damaged?.Invoke();

        if (animator != null && _hasHurtTrigger)
        {
            animator.SetTrigger("Hurt");
        }

        damageFeedback?.Play();
    }

    protected override void Die()
    {
        IsDead = true;
        Died?.Invoke();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (animator != null && _hasDeathTrigger)
        {
            animator.SetTrigger("Death");
        }

        damageFeedback?.Play();
        Debug.Log("Player died");
    }

    private void ApplyKnockback()
    {
        if (rb == null)
        {
            return;
        }

        var direction = transform.localScale.x > 0 ? -1f : 1f;
        rb.linearVelocity = new Vector2(direction * knockbackForceX, knockbackForceY);
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
