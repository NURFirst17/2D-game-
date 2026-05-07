using UnityEngine;

public class PlayerHealthTester : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            playerHealth.TakeDamage(1);
        }

        if (Input.GetKeyDown(KeyCode.J))
        {
            playerHealth.Heal(1);
        }
    }
}