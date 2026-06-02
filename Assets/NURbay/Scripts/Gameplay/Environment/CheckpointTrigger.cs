using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class CheckpointTrigger : MonoBehaviour, ICheckpointStateParticipant
{
    [System.Serializable]
    private sealed class CheckpointState
    {
        public bool IsActivated;
    }

    private static readonly int IsActivatedParameter = Animator.StringToHash("IsActivated");

    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private SpriteRenderer checkpointRenderer;
    [SerializeField] private Animator checkpointAnimator;
    [SerializeField] private Color activatedColor = Color.yellow;
    [SerializeField] private bool activateOnlyOnce = true;

    [Header("Rendering")]
    [SerializeField] private string sortingLayerName = "Environment";
    [SerializeField] private int orderInLayer = 100;

    private bool _isActivated;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;

        if (checkpointRenderer == null)
        {
            checkpointRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (checkpointAnimator == null)
        {
            checkpointAnimator = GetComponentInChildren<Animator>();
        }

        ApplyRenderingSettings();
        RefreshVisualState();
    }

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnValidate()
    {
        if (checkpointRenderer == null)
        {
            checkpointRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        ApplyRenderingSettings();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((_isActivated && activateOnlyOnce) || !IsPlayer(other))
        {
            return;
        }

        _isActivated = true;
        RefreshVisualState();

        var targetRespawnPoint = respawnPoint != null ? respawnPoint : transform;
        CheckpointSnapshotSystem.CaptureCurrentScene(targetRespawnPoint.position);
    }

    private bool IsPlayer(Collider2D other)
    {
        return other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag);
    }

    public string CaptureCheckpointState()
    {
        return JsonUtility.ToJson(new CheckpointState { IsActivated = _isActivated });
    }

    public void RestoreCheckpointState(string state)
    {
        var checkpointState = JsonUtility.FromJson<CheckpointState>(state);
        _isActivated = checkpointState.IsActivated;
        RefreshVisualState();
    }

    private void RefreshVisualState()
    {
        if (checkpointAnimator != null)
        {
            checkpointAnimator.SetBool(IsActivatedParameter, _isActivated);
        }

        if (_isActivated && checkpointRenderer != null)
        {
            checkpointRenderer.color = activatedColor;
        }
    }

    private void ApplyRenderingSettings()
    {
        if (checkpointRenderer == null)
        {
            return;
        }

        checkpointRenderer.sortingLayerName = sortingLayerName;
        checkpointRenderer.sortingOrder = orderInLayer;
    }

    private void OnDrawGizmos()
    {
        var targetRespawnPoint = respawnPoint != null ? respawnPoint : transform;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(targetRespawnPoint.position, 0.35f);
        Gizmos.DrawLine(targetRespawnPoint.position, targetRespawnPoint.position + Vector3.up * 0.75f);
    }
}
