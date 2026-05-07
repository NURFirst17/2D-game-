using UnityEngine;

public sealed class PlayerSpawnPoint : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool applyRotation;

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null)
        {
            Debug.LogWarning($"PlayerSpawnPoint could not find an object with tag '{playerTag}'.", this);
            return;
        }

        player.transform.position = transform.position;

        if (applyRotation)
        {
            player.transform.rotation = transform.rotation;
        }

        if (player.TryGetComponent<Rigidbody2D>(out var playerBody))
        {
            playerBody.linearVelocity = Vector2.zero;
            playerBody.angularVelocity = 0f;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.35f);
        Gizmos.DrawLine(transform.position, transform.position + transform.right * 0.6f);
    }
}
