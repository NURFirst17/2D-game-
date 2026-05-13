using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrup : MonoBehaviour
{
    [SerializeField] private string initialSceneName = "MainMenu";

    private SceneSwitcher _sceneSwitcher;

    private void Awake()
    {
        if (FindObjectsByType<Bootstrup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        RegisterServices();

        _sceneSwitcher = GetComponent<SceneSwitcher>();
        if (_sceneSwitcher == null)
        {
            _sceneSwitcher = gameObject.AddComponent<SceneSwitcher>();
        }

        ServiceLocator.Register(_sceneSwitcher);
        _sceneSwitcher.Initialize(initialSceneName);
    }

    private static void RegisterServices()
    {
        ServiceLocator.Clear();
        ServiceLocator.Register<IPrefabLoader>(new ResourcesPrefabLoader());
        ServiceLocator.Register<ILevelProgressionService>(new LevelProgressionService());
    }

    private void Start()
    {
        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.name == SceneSwitcher.BootstrapSceneName)
        {
            _sceneSwitcher.LoadInitialScene();
            return;
        }

        _sceneSwitcher.BindGameplayScene(activeScene);
    }
}
