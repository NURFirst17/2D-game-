using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PlayerHealth))]
public sealed class PlayerDeathRestart : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private float restartDelay = 1.5f;

    private Coroutine _restartRoutine;

    private void Awake()
    {
        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.Died += HandlePlayerDied;
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.Died -= HandlePlayerDied;
        }
    }

    private void HandlePlayerDied()
    {
        if (_restartRoutine != null)
        {
            return;
        }

        _restartRoutine = StartCoroutine(RestartAfterDelay());
    }

    private IEnumerator RestartAfterDelay()
    {
        if (restartDelay > 0f)
        {
            yield return new WaitForSeconds(restartDelay);
        }

        if (!ServiceLocator.TryResolve<SceneSwitcher>(out var sceneSwitcher))
        {
            sceneSwitcher = FindFirstObjectByType<SceneSwitcher>();
        }

        if (sceneSwitcher != null)
        {
            sceneSwitcher.ReloadCurrentScene();
            yield break;
        }

        var activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.name);
    }
}
