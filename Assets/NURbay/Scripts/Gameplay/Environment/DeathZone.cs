using UnityEngine;
[RequireComponent(typeof(Collider2D))]
public sealed class DeathZone : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    private bool _isTriggered;

    private void Reset()
    {
        var trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isTriggered || !other.CompareTag(playerTag))
        {
            return;
        }

        _isTriggered = true;

        if (!ServiceLocator.TryResolve<SceneSwitcher>(out var sceneSwitcher))
        {
            sceneSwitcher = FindFirstObjectByType<SceneSwitcher>();
        }

        if (sceneSwitcher != null)
        {
            sceneSwitcher.ReloadCurrentScene();
            return;
        }

        SceneTransition.ReloadCurrentScene();
    }
}
