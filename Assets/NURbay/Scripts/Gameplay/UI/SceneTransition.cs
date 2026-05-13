using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class SceneTransition : MonoBehaviour
{
    private const string TransitionObjectName = "SceneTransition";
    private const int SortingOrder = 32767;

    private static SceneTransition _instance;

    [SerializeField] private float fadeDuration = 0.8f;
    [SerializeField] private float blackScreenDelay = 0.4f;

    private CanvasGroup _canvasGroup;
    private bool _isLoading;

    public static void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return;
        }

        Instance.StartLoadScene(sceneName);
    }

    public static void LoadScene(int sceneBuildIndex)
    {
        if (sceneBuildIndex < 0 || sceneBuildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            return;
        }

        Instance.StartLoadScene(sceneBuildIndex);
    }

    public static void ReloadCurrentScene()
    {
        LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private static SceneTransition Instance
    {
        get
        {
            if (_instance != null)
            {
                return _instance;
            }

            var transitionObject = new GameObject(TransitionObjectName);
            var sceneTransition = transitionObject.AddComponent<SceneTransition>();
            return _instance != null ? _instance : sceneTransition;
        }
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

        if (_canvasGroup == null)
        {
            CreateFadeCanvas();
        }
    }

    private void StartLoadScene(string sceneName)
    {
        if (_isLoading)
        {
            return;
        }

        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private void StartLoadScene(int sceneBuildIndex)
    {
        if (_isLoading)
        {
            return;
        }

        StartCoroutine(LoadSceneRoutine(sceneBuildIndex));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        _isLoading = true;
        _canvasGroup.blocksRaycasts = true;

        yield return FadeTo(1f);
        yield return WaitOnBlackScreen();

        var loadOperation = SceneManager.LoadSceneAsync(sceneName);
        if (loadOperation == null)
        {
            _canvasGroup.blocksRaycasts = false;
            _isLoading = false;
            yield break;
        }

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        yield return FadeTo(0f);

        _canvasGroup.blocksRaycasts = false;
        _isLoading = false;
    }

    private IEnumerator LoadSceneRoutine(int sceneBuildIndex)
    {
        _isLoading = true;
        _canvasGroup.blocksRaycasts = true;

        yield return FadeTo(1f);
        yield return WaitOnBlackScreen();

        var loadOperation = SceneManager.LoadSceneAsync(sceneBuildIndex);
        if (loadOperation == null)
        {
            _canvasGroup.blocksRaycasts = false;
            _isLoading = false;
            yield break;
        }

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        yield return FadeTo(0f);

        _canvasGroup.blocksRaycasts = false;
        _isLoading = false;
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        var startAlpha = _canvasGroup.alpha;
        var elapsedTime = 0f;

        if (fadeDuration <= 0f)
        {
            _canvasGroup.alpha = targetAlpha;
            yield break;
        }

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / fadeDuration);
            yield return null;
        }

        _canvasGroup.alpha = targetAlpha;
    }

    private IEnumerator WaitOnBlackScreen()
    {
        if (blackScreenDelay <= 0f)
        {
            yield break;
        }

        yield return new WaitForSecondsRealtime(blackScreenDelay);
    }

    private void CreateFadeCanvas()
    {
        var canvasObject = new GameObject("FadeCanvas");
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;

        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        _canvasGroup = canvasObject.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;

        var imageObject = new GameObject("FadeImage");
        imageObject.transform.SetParent(canvasObject.transform, false);

        var image = imageObject.AddComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;

        var rectTransform = image.rectTransform;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
