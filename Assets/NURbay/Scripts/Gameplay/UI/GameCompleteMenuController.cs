using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class GameCompleteMenuController : MonoBehaviour
{
    private const string FinalLevelSceneName = "Level_04_Boss";
    private const string FirstLevelSceneName = "Level_01";
    private const string MainMenuSceneName = "MainMenu";
    private const int SortingOrder = 32500;

    private static GameCompleteMenuController _instance;

    private GameObject _menuRoot;

    public static bool IsVisible => _instance != null &&
                                    _instance._menuRoot != null &&
                                    _instance._menuRoot.activeSelf;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (_instance != null)
        {
            return;
        }

        var menuObject = new GameObject(nameof(GameCompleteMenuController));
        _instance = menuObject.AddComponent<GameCompleteMenuController>();
        DontDestroyOnLoad(menuObject);
    }

    public static bool TryShowForCurrentScene()
    {
        if (SceneManager.GetActiveScene().name != FinalLevelSceneName)
        {
            return false;
        }

        EnsureInstance();
        _instance.Show();
        return true;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        CreateMenu();
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Update()
    {
        if (IsVisible && Time.timeScale != 0f)
        {
            Time.timeScale = 0f;
        }
    }

    private void Show()
    {
        Time.timeScale = 0f;
        _menuRoot.SetActive(true);
    }

    private void RestartGame()
    {
        _menuRoot.SetActive(false);
        Time.timeScale = 1f;
        ContinueGameProgress.BeginNewGame();
        TutorialHintController.PrepareForNewGame();
        SceneTransition.LoadScene(FirstLevelSceneName);
    }

    private void ExitToMainMenu()
    {
        _menuRoot.SetActive(false);
        Time.timeScale = 1f;
        SceneTransition.LoadScene(MainMenuSceneName);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        _menuRoot.SetActive(false);

        if (!TutorialHintController.IsVisible)
        {
            Time.timeScale = 1f;
        }
    }

    private void CreateMenu()
    {
        var canvasObject = new GameObject("GameCompleteCanvas");
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        _menuRoot = CreateUiObject("GameCompleteMenu", canvasObject.transform);
        StretchToParent(_menuRoot.GetComponent<RectTransform>());

        var backdrop = _menuRoot.AddComponent<Image>();
        backdrop.color = new Color(0f, 0f, 0f, 0.76f);

        var panel = CreateUiObject("Panel", _menuRoot.transform);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(620f, 470f);

        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.09f, 0.13f, 0.98f);

        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(52, 52, 38, 38);
        layout.spacing = 20f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        CreateLabel(panel.transform, "CONGRATULATIONS!", 40, 82f, FontStyle.Bold);
        CreateLabel(panel.transform, "You have completed the game.", 26, 74f, FontStyle.Normal);
        CreateButton(panel.transform, "RestartGameButton", "Restart Game", RestartGame);
        CreateButton(panel.transform, "MainMenuButton", "Main Menu", ExitToMainMenu);

        EnsureEventSystem();
        _menuRoot.SetActive(false);
    }

    private static void CreateLabel(Transform parent, string value, int fontSize, float height, FontStyle fontStyle)
    {
        var labelObject = CreateUiObject("Text", parent);
        var label = labelObject.AddComponent<Text>();
        ConfigureText(label, value, fontSize);
        label.fontStyle = fontStyle;

        var layout = labelObject.AddComponent<LayoutElement>();
        layout.preferredHeight = height;
    }

    private static void CreateButton(Transform parent, string objectName, string caption, UnityEngine.Events.UnityAction action)
    {
        var buttonObject = CreateUiObject(objectName, parent);
        var image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.18f, 0.22f, 0.32f, 1f);

        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        var colors = button.colors;
        colors.highlightedColor = new Color(0.3f, 0.38f, 0.56f, 1f);
        colors.pressedColor = new Color(0.12f, 0.15f, 0.24f, 1f);
        button.colors = colors;

        var layout = buttonObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 68f;

        var labelObject = CreateUiObject("Text", buttonObject.transform);
        StretchToParent(labelObject.GetComponent<RectTransform>());
        var label = labelObject.AddComponent<Text>();
        ConfigureText(label, caption, 25);
    }

    private static void ConfigureText(Text label, string value, int fontSize)
    {
        label.text = value;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
    }

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        var gameObject = new GameObject(objectName, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        if (_instance == this)
        {
            _instance = null;
            Time.timeScale = 1f;
        }
    }
}
