using UnityEngine;
using UnityEngine.SceneManagement;

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

        if (GameCompleteMenuController.TryShowForCurrentScene())
        {
            return;
        }

        if (!ServiceLocator.TryResolve<SceneSwitcher>(out var sceneSwitcher))
        {
            sceneSwitcher = Object.FindFirstObjectByType<SceneSwitcher>();
        }

        if (sceneSwitcher == null)
        {
            if (loadNextLevelAutomatically)
            {
                LoadNextSceneDirectly();
            }

            return;
        }

        if (loadNextLevelAutomatically)
        {
            sceneSwitcher.CompleteCurrentLevelAndLoadNext();
            return;
        }

        if (ServiceLocator.TryResolve<ILevelProgressionService>(out var levelProgressionService))
        {
            levelProgressionService.MarkLevelCompleted(SceneManager.GetActiveScene().name);
        }
    }

    private static void LoadNextSceneDirectly()
    {
        var currentScene = SceneManager.GetActiveScene();
        var nextSceneIndex = currentScene.buildIndex + 1;

        if (nextSceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            return;
        }

        SceneTransition.LoadScene(nextSceneIndex);
    }
}
