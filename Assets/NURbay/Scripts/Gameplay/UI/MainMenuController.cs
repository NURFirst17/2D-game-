using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class MainMenuController : MonoBehaviour
{
    [SerializeField] private string playSceneName = "Level_01";

    public void Play()
    {
        if (string.IsNullOrWhiteSpace(playSceneName))
        {
            Debug.LogWarning("Play scene name is empty.");
            return;
        }

        SceneTransition.LoadScene(playSceneName);
    }

    public void Exit()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
