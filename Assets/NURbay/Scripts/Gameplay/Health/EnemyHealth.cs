using UnityEngine;

public class EnemyHealth : EntityHealth
{
    public event System.Action Damaged;
    public event System.Action Died;

    [Header("Feedback")]
    [SerializeField] private Animator animator;
    [SerializeField] private DamageFlashFeedback damageFeedback;
    [SerializeField] private float destroyDelay = 0.5f;
    private bool _hasHurtTrigger;
    private bool _hasDeathTrigger;

    protected override void Awake()
    {
        base.Awake();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
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

    protected override void OnDamageTaken()
    {
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

        if (animator != null && _hasDeathTrigger)
        {
            animator.SetTrigger("Death");
        }

        damageFeedback?.Play();
        Debug.Log(gameObject.name + " died");
        Destroy(gameObject, destroyDelay);
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
