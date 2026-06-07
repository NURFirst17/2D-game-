using UnityEngine;

public class BossHealthBar : MonoBehaviour
{
    [SerializeField] private Transform fillPivot;

    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = fillPivot.localScale;
    }

    public void UpdateHealth(int currentHealth, int maxHealth)
    {
        float percent = (float)currentHealth / maxHealth;

        fillPivot.localScale = new Vector3(
            percent,
            originalScale.y,
            originalScale.z
        );
    }
}