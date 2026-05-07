using System.Collections.Generic;

public interface ILevelProgressionService
{
    IReadOnlyList<string> Levels { get; }
    string FirstLevelName { get; }
    bool IsLevelUnlocked(string levelName);
    bool TryGetNextLevel(string currentLevelName, out string nextLevelName);
    void MarkLevelCompleted(string levelName);
}
