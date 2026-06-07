using System.Collections;
using UnityEngine;

public class BossHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 300;
    [SerializeField] private BossHealthBar healthBar;

    [Header("Hurt Visual")]
    [SerializeField] private float hurtFlashTime = 0.15f;
    [SerializeField] private Color hurtColor = Color.red;

    private int currentHealth;
    private bool isDead;
    private bool phaseTwoStarted;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private GolemBossAI bossAI;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;

    private void Awake()
    {
        currentHealth = maxHealth;
        isDead = false;
        phaseTwoStarted = false;

        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bossAI = GetComponent<GolemBossAI>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    private void Start()
    {
        if (healthBar != null)
            healthBar.UpdateHealth(currentHealth, maxHealth);
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        currentHealth -= damage;

        if (currentHealth < 0)
            currentHealth = 0;

        if (healthBar != null)
            healthBar.UpdateHealth(currentHealth, maxHealth);

        Debug.Log("Boss HP: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        if (!phaseTwoStarted && currentHealth <= maxHealth / 2)
        {
            phaseTwoStarted = true;

            if (bossAI != null)
                bossAI.StartPhaseTwo();

            StartCoroutine(HurtFlash());
            return;
        }

        if (!IsPlayingAttackAnimation())
        {
            if (animator != null)
                animator.SetTrigger("Hurt");
        }

        StartCoroutine(HurtFlash());
    }

    private bool IsPlayingAttackAnimation()
    {
        if (animator == null)
            return false;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        return stateInfo.IsName("Golem_MeleeAttack") ||
               stateInfo.IsName("Golem_ArmShoot") ||
               stateInfo.IsName("Golem_ChargeLaser") ||
               stateInfo.IsName("Golem_PhaseTwo");
    }

    private IEnumerator HurtFlash()
    {
        if (spriteRenderer == null)
            yield break;

        spriteRenderer.color = hurtColor;

        yield return new WaitForSeconds(hurtFlashTime);

        spriteRenderer.color = originalColor;
    }

    private void Die()
    {
        isDead = true;

        if (bossAI != null)
            bossAI.enabled = false;

        if (animator != null)
            animator.SetTrigger("Death");

        Collider2D mainCollider = GetComponent<Collider2D>();
        if (mainCollider != null)
            mainCollider.isTrigger = true;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 4f;
            rb.freezeRotation = true;
        }
    }
}