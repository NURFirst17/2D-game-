using UnityEngine;

public static class GameEntryPoint
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (Object.FindFirstObjectByType<Bootstrup>() != null)
        {
            return;
        }

        var bootstrapObject = new GameObject(nameof(Bootstrup));
        bootstrapObject.AddComponent<Bootstrup>();
        Object.DontDestroyOnLoad(bootstrapObject);
    }
}
