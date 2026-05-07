using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LevelCompleteTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool loadNextLevelAutomatically = true;

    private void Reset()
    {
        var trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        if (!ServiceLocator.TryResolve<SceneSwitcher>(out var sceneSwitcher))
        {
            sceneSwitcher = Object.FindFirstObjectByType<SceneSwitcher>();
        }

        if (sceneSwitcher == null)
        {
            return;
        }

        if (loadNextLevelAutomatically)
        {
            sceneSwitcher.CompleteCurrentLevelAndLoadNext();
            return;
        }

        if (ServiceLocator.TryResolve<ILevelProgressionService>(out var levelProgressionService))
        {
            levelProgressionService.MarkLevelCompleted(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }
}
