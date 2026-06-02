using UnityEngine;

public class LightCrystal : MonoBehaviour, ILightInteractable, ICheckpointStateParticipant
{
    [System.Serializable]
    private sealed class CheckpointState
    {
        public bool IsActivated;
        public Color Color;
    }

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

    public string CaptureCheckpointState()
    {
        return JsonUtility.ToJson(new CheckpointState
        {
            IsActivated = isActivated,
            Color = spriteRenderer != null ? spriteRenderer.color : Color.white
        });
    }

    public void RestoreCheckpointState(string state)
    {
        var checkpointState = JsonUtility.FromJson<CheckpointState>(state);
        isActivated = checkpointState.IsActivated;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = checkpointState.Color;
        }
    }
}
