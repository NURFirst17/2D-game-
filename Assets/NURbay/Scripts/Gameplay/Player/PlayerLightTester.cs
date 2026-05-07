using UnityEngine;

public class PlayerLightTester : MonoBehaviour
{
    [SerializeField] private PlayerLight playerLight;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            bool used = playerLight.TryUseLight(20f);
            Debug.Log("Use 20 light: " + used + " | Current: " + playerLight.CurrentLight);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            playerLight.RecoverLight(15f);
            Debug.Log("Recover 15 light | Current: " + playerLight.CurrentLight);
        }
    }
}