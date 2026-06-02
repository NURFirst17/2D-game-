using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class TutorialHintController : MonoBehaviour
{
    private const string FirstLevelSceneName = "Level_01";
    private const string MainMenuSceneName = "MainMenu";
    private const string GameplayScenePrefix = "Level_";
    private const int SortingOrder = 31000;

    private static TutorialHintController _instance;

    private GameObject _hintRoot;
    private bool _hasShownAutomatically;

    public static bool IsVisible => _instance != null &&
                                    _instance._hintRoot != null &&
                                    _instance._hintRoot.activeSelf;

    public static void PrepareForNewGame()
    {
        if (_instance != null)
        {
            _instance._hasShownAutomatically = false;
        }
    }

    public static void SkipForContinue()
    {
        if (_instance != null)
        {
            _instance._hasShownAutomatically = true;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (_instance != null)
        {
            return;
        }

        var tutorialObject = new GameObject(nameof(TutorialHintController));
        _instance = tutorialObject.AddComponent<TutorialHintController>();
        DontDestroyOnLoad(tutorialObject);
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
        CreateHint();
        SceneManager.sceneLoaded += HandleSceneLoaded;
        RefreshForScene(SceneManager.GetActiveScene());
    }

    private void Update()
    {
        if (!IsGameplayScene(SceneManager.GetActiveScene()))
        {
            return;
        }

        if (!_hintRoot.activeSelf)
        {
            return;
        }

        if (Time.timeScale != 0f)
        {
            Time.timeScale = 0f;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            StartGame();
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        RefreshForScene(scene);
    }

    private void RefreshForScene(Scene scene)
    {
        enabled = IsGameplayScene(scene);

        if (scene.IsValid() && scene.name == MainMenuSceneName)
        {
            _hasShownAutomatically = false;
        }

        if (!enabled)
        {
            SetVisible(false);
            return;
        }

        if (!_hasShownAutomatically && scene.name == FirstLevelSceneName)
        {
            _hasShownAutomatically = true;
            SetVisible(true);
            return;
        }

        SetVisible(false);
    }

    private void StartGame()
    {
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (_hintRoot != null)
        {
            _hintRoot.SetActive(visible);
        }

        Time.timeScale = visible ? 0f : 1f;
    }

    private void CreateHint()
    {
        var canvasObject = new GameObject("TutorialHintCanvas");
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        _hintRoot = CreateUiObject("TutorialHint", canvasObject.transform);
        StretchToParent(_hintRoot.GetComponent<RectTransform>());

        var backdrop = _hintRoot.AddComponent<Image>();
        backdrop.color = new Color(0f, 0f, 0f, 0.72f);

        var panel = CreateUiObject("Panel", _hintRoot.transform);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(620f, 590f);

        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.09f, 0.13f, 0.96f);

        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(52, 52, 34, 34);
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        CreateLabel(panel.transform, "CONTROLS", 42, 76f, FontStyle.Bold);
        CreateLabel(
            panel.transform,
            "A / D or Arrow Keys  -  Move\n" +
            "Space  -  Jump\n" +
            "J  -  Attack\n" +
            "E  -  Interact\n" +
            "Esc  -  Pause",
            25,
            280f,
            FontStyle.Normal);
        CreateButton(panel.transform, "StartButton", "Start Game", StartGame);
        CreateLabel(panel.transform, "Press Enter to start", 18, 36f, FontStyle.Italic);

        EnsureEventSystem();
        _hintRoot.SetActive(false);
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

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        var gameObject = new GameObject(objectName, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static bool IsGameplayScene(Scene scene)
    {
        return scene.IsValid() && scene.name.StartsWith(GameplayScenePrefix);
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
