using UnityEngine;

public class LevelSelectButton : MonoBehaviour
{
    [SerializeField] private string levelName;

    public void LoadLevel()
    {
        if (string.IsNullOrWhiteSpace(levelName))
        {
            return;
        }

        var sceneSwitcher = Object.FindFirstObjectByType<SceneSwitcher>();
        if (sceneSwitcher == null)
        {
            SceneTransition.LoadScene(levelName);
            return;
        }

        sceneSwitcher.LoadScene(levelName);
    }
}
