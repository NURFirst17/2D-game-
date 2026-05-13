public static class GameEntryPoint
{
    /*
    Bootstrap startup is disabled because the project now starts from MainMenu
    and loads levels directly from UI/buttons.
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
    */
}
