using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class LevelProgressionService : ILevelProgressionService
{
    private const string CompletedLevelKeyPrefix = "level_completed_";

    private readonly List<string> _levels = new();

    public IReadOnlyList<string> Levels => _levels;
    public string FirstLevelName => _levels.Count > 0 ? _levels[0] : string.Empty;

    public LevelProgressionService()
    {
        BuildLevelList();
    }

    public bool IsLevelUnlocked(string levelName)
    {
        if (string.IsNullOrWhiteSpace(levelName))
        {
            return false;
        }

        var levelIndex = _levels.IndexOf(levelName);
        if (levelIndex < 0)
        {
            return false;
        }

        if (levelIndex == 0)
        {
            return true;
        }

        var previousLevel = _levels[levelIndex - 1];
        return PlayerPrefs.GetInt(GetCompletedLevelKey(previousLevel), 0) == 1;
    }

    public bool TryGetNextLevel(string currentLevelName, out string nextLevelName)
    {
        var levelIndex = _levels.IndexOf(currentLevelName);
        if (levelIndex < 0 || levelIndex + 1 >= _levels.Count)
        {
            nextLevelName = string.Empty;
            return false;
        }

        nextLevelName = _levels[levelIndex + 1];
        return true;
    }

    public void MarkLevelCompleted(string levelName)
    {
        if (string.IsNullOrWhiteSpace(levelName))
        {
            return;
        }

        PlayerPrefs.SetInt(GetCompletedLevelKey(levelName), 1);

        if (TryGetNextLevel(levelName, out var nextLevelName))
        {
            PlayerPrefs.SetInt(GetCompletedLevelKey(nextLevelName), 0);
        }

        PlayerPrefs.Save();
    }

    private void BuildLevelList()
    {
        _levels.Clear();

        for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            var scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            var sceneName = Path.GetFileNameWithoutExtension(scenePath);
            if (sceneName == SceneSwitcher.BootstrapSceneName)
            {
                continue;
            }

            _levels.Add(sceneName);
        }
    }

    private static string GetCompletedLevelKey(string levelName)
    {
        return CompletedLevelKeyPrefix + levelName;
    }
}
