using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;

    private int currentHealth;
    private bool isDead;
    private Animator animator;

    public bool IsDead => isDead;

    private void Awake()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
            return;
        }

        if (animator != null)
        {
            animator.SetTrigger("Hurt");
        }
    }

    private void Die()
    {
        isDead = true;

        if (animator != null)
        {
            animator.SetTrigger("Death");
        }

        Debug.Log(gameObject.name + " died");

        Destroy(gameObject, 0.5f);
    }
}