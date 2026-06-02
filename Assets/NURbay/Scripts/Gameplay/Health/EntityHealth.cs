using UnityEngine;

public abstract class EntityHealth : MonoBehaviour, IDamageable, ICheckpointStateParticipant
{
    [System.Serializable]
    private sealed class CheckpointState
    {
        public int CurrentHealth;
        public bool IsDead;
    }

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

    public string CaptureCheckpointState()
    {
        return JsonUtility.ToJson(new CheckpointState
        {
            CurrentHealth = CurrentHealth,
            IsDead = IsDead
        });
    }

    public void RestoreCheckpointState(string state)
    {
        var checkpointState = JsonUtility.FromJson<CheckpointState>(state);
        CurrentHealth = Mathf.Clamp(checkpointState.CurrentHealth, 0, maxHealth);
        IsDead = checkpointState.IsDead;
    }

    protected abstract void OnDamageTaken();
    protected abstract void Die();
}
