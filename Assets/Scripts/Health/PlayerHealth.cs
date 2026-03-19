using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private float invincibilityDuration = 1f;

    [Header("Knockback")]
    [SerializeField] private float knockbackForceX = 5f;
    [SerializeField] private float knockbackForceY = 4f;

    private int currentHealth;
    private float invincibilityTimer;
    private bool isDead;

    private Animator animator;
    private Rigidbody2D rb;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
    }

    private void Update()
    {
        if (invincibilityTimer > 0f)
        {
            invincibilityTimer -= Time.deltaTime;
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        if (invincibilityTimer > 0f)
            return;

        currentHealth -= damage;

        if (currentHealth < 0)
            currentHealth = 0;

        invincibilityTimer = invincibilityDuration;

        ApplyKnockback();

        if (currentHealth > 0)
        {
            if (animator != null)
            {
                animator.SetTrigger("Hurt");
            }
        }
        else
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (isDead)
            return;

        currentHealth += amount;

        if (currentHealth > maxHealth)
            currentHealth = maxHealth;
    }

    private void ApplyKnockback()
    {
        if (rb == null)
            return;

        float direction = transform.localScale.x > 0 ? -1f : 1f;
        rb.linearVelocity = new Vector2(direction * knockbackForceX, knockbackForceY);
    }

    private void Die()
    {
        isDead = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (animator != null)
        {
            animator.SetTrigger("Death");
        }

        Debug.Log("Player died");
    }
}