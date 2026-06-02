using UnityEngine;
using UnityEngine.SceneManagement;

public static class ContinueGameProgress
{
    private const string LastSceneKey = "continue_last_scene";
    private const string GameplayScenePrefix = "Level_";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    public static bool TryGetSavedScene(out string sceneName)
    {
        sceneName = PlayerPrefs.GetString(LastSceneKey, string.Empty);
        return IsGameplayScene(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName);
    }

    public static void BeginNewGame()
    {
        PlayerPrefs.DeleteKey(LastSceneKey);
        PlayerPrefs.Save();
        CheckpointSnapshotSystem.ClearSavedCheckpoint();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (!scene.IsValid() || !IsGameplayScene(scene.name))
        {
            return;
        }

        PlayerPrefs.SetString(LastSceneKey, scene.name);
        PlayerPrefs.Save();
    }

    private static bool IsGameplayScene(string sceneName)
    {
        return !string.IsNullOrWhiteSpace(sceneName) && sceneName.StartsWith(GameplayScenePrefix);
    }
}
