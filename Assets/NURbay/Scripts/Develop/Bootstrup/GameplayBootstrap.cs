using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameplayBootstrap : MonoBehaviour
{
    private SceneSwitcher _sceneSwitcher;
    private bool _isInitialized;

    public SceneSwitcher SceneSwitcher => _sceneSwitcher;

    public static GameplayBootstrap EnsureForScene(Scene scene, SceneSwitcher sceneSwitcher)
    {
        foreach (var rootGameObject in scene.GetRootGameObjects())
        {
            var existingBootstrap = rootGameObject.GetComponentInChildren<GameplayBootstrap>(true);
            if (existingBootstrap != null)
            {
                existingBootstrap.Initialize(sceneSwitcher);
                return existingBootstrap;
            }
        }

        var bootstrapObject = new GameObject(nameof(GameplayBootstrap));
        SceneManager.MoveGameObjectToScene(bootstrapObject, scene);

        var gameplayBootstrap = bootstrapObject.AddComponent<GameplayBootstrap>();
        gameplayBootstrap.Initialize(sceneSwitcher);
        return gameplayBootstrap;
    }

    public void Initialize(SceneSwitcher sceneSwitcher)
    {
        if (_isInitialized)
        {
            return;
        }

        _sceneSwitcher = sceneSwitcher;
        _isInitialized = true;

        Debug.Log($"Gameplay bootstrap initialized for scene '{gameObject.scene.name}'.");
    }
}
