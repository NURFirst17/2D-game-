using TMPro;
using UnityEngine;

public sealed class PlayerHealthView : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private string format = "HP: {0}/{1}";

    private int _lastCurrentHealth = -1;
    private int _lastMaxHealth = -1;

    private void Awake()
    {
        if (healthText == null)
        {
            healthText = GetComponent<TMP_Text>();
        }

        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        }
    }

    private void OnEnable()
    {
        Refresh(force: true);
    }

    private void Update()
    {
        Refresh(force: false);
    }

    private void Refresh(bool force)
    {
        if (healthText == null || playerHealth == null)
        {
            return;
        }

        var currentHealth = playerHealth.CurrentHealthValue;
        var maxHealth = playerHealth.MaxHealth;

        if (!force && currentHealth == _lastCurrentHealth && maxHealth == _lastMaxHealth)
        {
            return;
        }

        _lastCurrentHealth = currentHealth;
        _lastMaxHealth = maxHealth;
        healthText.text = string.Format(format, currentHealth, maxHealth);
    }
}
