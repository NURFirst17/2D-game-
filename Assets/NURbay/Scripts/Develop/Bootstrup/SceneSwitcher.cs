using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class SceneSwitcher : MonoBehaviour
{
    public const string BootstrapSceneName = "Bootstrup";

    private string _initialSceneName = "Gameplay";
    private Coroutine _loadingRoutine;
    private ILevelProgressionService _levelProgressionService;

    public bool IsLoading => _loadingRoutine != null;

    public void Initialize(string initialSceneName)
    {
        if (!string.IsNullOrWhiteSpace(initialSceneName))
        {
            _initialSceneName = initialSceneName;
        }

        ServiceLocator.TryResolve<ILevelProgressionService>(out _levelProgressionService);

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public void LoadInitialScene()
    {
        LoadScene(_initialSceneName);
    }

    public void LoadScene(string sceneName)
    {
        if (IsLoading || string.IsNullOrWhiteSpace(sceneName))
        {
            return;
        }

        if (_levelProgressionService != null && !_levelProgressionService.IsLevelUnlocked(sceneName))
        {
            return;
        }

        if (SceneManager.GetActiveScene().name == sceneName)
        {
            BindGameplayScene(SceneManager.GetActiveScene());
            return;
        }

        _loadingRoutine = StartCoroutine(LoadSceneRoutine(sceneName));
    }

    public void ReloadCurrentScene()
    {
        if (IsLoading)
        {
            return;
        }

        _loadingRoutine = StartCoroutine(LoadSceneRoutine(SceneManager.GetActiveScene().name));
    }

    public void LoadNextScene()
    {
        if (_levelProgressionService == null)
        {
            return;
        }

        if (!_levelProgressionService.TryGetNextLevel(SceneManager.GetActiveScene().name, out var nextSceneName))
        {
            return;
        }

        LoadScene(nextSceneName);
    }

    public void CompleteCurrentLevelAndLoadNext()
    {
        if (_levelProgressionService == null)
        {
            return;
        }

        var currentSceneName = SceneManager.GetActiveScene().name;
        _levelProgressionService.MarkLevelCompleted(currentSceneName);

        if (_levelProgressionService.TryGetNextLevel(currentSceneName, out var nextSceneName))
        {
            LoadScene(nextSceneName);
        }
    }

    public void BindGameplayScene(Scene scene)
    {
        if (!scene.IsValid() || scene.name == BootstrapSceneName)
        {
            return;
        }

        GameplayBootstrap.EnsureForScene(scene, this);
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        var asyncOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        if (asyncOperation == null)
        {
            _loadingRoutine = null;
            yield break;
        }

        while (!asyncOperation.isDone)
        {
            yield return null;
        }

        _loadingRoutine = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        BindGameplayScene(scene);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
