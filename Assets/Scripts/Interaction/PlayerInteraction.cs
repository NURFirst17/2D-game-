using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private float interactionRange = 0.8f;
    [SerializeField] private LayerMask interactableLayer;

    private PlayerHealth playerHealth;
    private PlayerLight playerLight;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerLight = GetComponent<PlayerLight>();
    }

    private void Update()
    {
        if (playerHealth != null && playerHealth.IsDead)
            return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        Collider2D hit = Physics2D.OverlapCircle(
            interactionPoint.position,
            interactionRange,
            interactableLayer
        );

        if (hit == null)
            return;

        ILightInteractable interactable = hit.GetComponent<ILightInteractable>();

        if (interactable != null)
        {
            interactable.Interact(playerLight);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (interactionPoint == null)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(interactionPoint.position, interactionRange);
    }
}