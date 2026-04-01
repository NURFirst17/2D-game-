using UnityEngine;

public class PlayerLight : MonoBehaviour
{
    [Header("Light Settings")]
    [SerializeField] private float maxLight = 100f;
    [SerializeField] private float currentLight = 100f;
    [SerializeField] private float passiveRecoveryRate = 5f;

    private PlayerHealth playerHealth;

    public float CurrentLight => currentLight;
    public float MaxLight => maxLight;
    public bool HasLight => currentLight > 0f;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        currentLight = maxLight;
    }

    private void Update()
    {
        if (playerHealth != null && playerHealth.IsDead)
            return;

        RecoverLight(passiveRecoveryRate * Time.deltaTime);
    }

    public bool HasEnoughLight(float amount)
    {
        return currentLight >= amount;
    }

    public bool TryUseLight(float amount)
    {
        if (!HasEnoughLight(amount))
            return false;

        currentLight -= amount;

        if (currentLight < 0f)
            currentLight = 0f;

        return true;
    }

    public void RecoverLight(float amount)
    {
        currentLight += amount;

        if (currentLight > maxLight)
            currentLight = maxLight;
    }

    public void SetLight(float amount)
    {
        currentLight = Mathf.Clamp(amount, 0f, maxLight);
    }
}