using UnityEngine;

public class LightCrystal : MonoBehaviour, ILightInteractable
{
    [Header("Light Interaction")]
    [SerializeField] private float lightCost = 25f;
    [SerializeField] private bool oneTimeUse = true;

    private bool isActivated;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Interact(PlayerLight playerLight)
    {
        if (isActivated && oneTimeUse)
            return;

        if (playerLight == null)
            return;

        if (!playerLight.TryUseLight(lightCost))
        {
            Debug.Log("Not enough light to activate object.");
            return;
        }

        Activate();
    }

    private void Activate()
    {
        isActivated = true;

        Debug.Log(gameObject.name + " activated by light.");

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.yellow;
        }
    }
}