using UnityEngine;

public abstract class EntityHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 3;

    protected int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsDead { get; protected set; }

    protected virtual void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public virtual void TakeDamage(int damage)
    {
        if (IsDead || damage <= 0)
        {
            return;
        }

        CurrentHealth -= damage;
        if (CurrentHealth < 0)
        {
            CurrentHealth = 0;
        }

        if (CurrentHealth == 0)
        {
            Die();
            return;
        }

        OnDamageTaken();
    }

    protected void RestoreHealth(int amount)
    {
        if (IsDead || amount <= 0)
        {
            return;
        }

        CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
    }

    protected abstract void OnDamageTaken();
    protected abstract void Die();
}
