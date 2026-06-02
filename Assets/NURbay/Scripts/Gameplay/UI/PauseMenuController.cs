using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class PauseMenuController : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";
    private const string GameplayScenePrefix = "Level_";
    private const int SortingOrder = 32000;

    private static PauseMenuController _instance;

    private GameObject _menuRoot;
    private bool _isPaused;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (_instance != null)
        {
            return;
        }

        var pauseObject = new GameObject(nameof(PauseMenuController));
        _instance = pauseObject.AddComponent<PauseMenuController>();
        DontDestroyOnLoad(pauseObject);
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
        RefreshAvailability(SceneManager.GetActiveScene());
    }

    private void Update()
    {
        if (!IsGameplayScene(SceneManager.GetActiveScene()) ||
            TutorialHintController.IsVisible ||
            GameCompleteMenuController.IsVisible ||
            !Input.GetKeyDown(KeyCode.Escape))
        {
            return;
        }

        if (_isPaused)
        {
            Continue();
        }
        else
        {
            Pause();
        }
    }

    public void Continue()
    {
        SetPaused(false);
    }

    public void RestartLevel()
    {
        SetPaused(false);

        if (!ServiceLocator.TryResolve<SceneSwitcher>(out var sceneSwitcher))
        {
            sceneSwitcher = FindFirstObjectByType<SceneSwitcher>();
        }

        if (sceneSwitcher != null)
        {
            sceneSwitcher.ReloadCurrentScene();
            return;
        }

        SceneTransition.ReloadCurrentScene();
    }

    public void ExitToMainMenu()
    {
        SetPaused(false);
        SceneTransition.LoadScene(MainMenuSceneName);
    }

    private void Pause()
    {
        SetPaused(true);
    }

    private void SetPaused(bool paused)
    {
        _isPaused = paused;
        Time.timeScale = paused ? 0f : 1f;

        if (_menuRoot != null)
        {
            _menuRoot.SetActive(paused);
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        SetPaused(false);
        RefreshAvailability(scene);

        if (TutorialHintController.IsVisible)
        {
            Time.timeScale = 0f;
        }
    }

        private void RefreshAvailability(Scene scene)
        {
            enabled = IsGameplayScene(scene);

            if (!enabled)
            {
                SetPaused(false);
                return;
            }

            EnsureEventSystem();
        }

    private static bool IsGameplayScene(Scene scene)
    {
        return scene.IsValid() && scene.name.StartsWith(GameplayScenePrefix);
    }

    private void CreateMenu()
    {
        var canvasObject = new GameObject("PauseCanvas");
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        _menuRoot = CreateUiObject("PauseMenu", canvasObject.transform);
        StretchToParent(_menuRoot.GetComponent<RectTransform>());

        var backdrop = _menuRoot.AddComponent<Image>();
        backdrop.color = new Color(0f, 0f, 0f, 0.72f);

        var panel = CreateUiObject("Panel", _menuRoot.transform);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(420f, 430f);

        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.09f, 0.13f, 0.96f);

        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(42, 42, 34, 34);
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        CreateLabel(panel.transform, "PAUSE", 42, 84f);
        CreateButton(panel.transform, "ContinueButton", "Continue", Continue);
        CreateButton(panel.transform, "RestartButton", "Restart Level", RestartLevel);
        CreateButton(panel.transform, "MainMenuButton", "Main Menu", ExitToMainMenu);

        EnsureEventSystem();
        _menuRoot.SetActive(false);
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

    private static void CreateLabel(Transform parent, string text, int fontSize, float height)
    {
        var labelObject = CreateUiObject("Title", parent);
        var label = labelObject.AddComponent<Text>();
        ConfigureText(label, text, fontSize);
        label.fontStyle = FontStyle.Bold;

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

    private static void ConfigureText(Text text, string value, int fontSize)
    {
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
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
