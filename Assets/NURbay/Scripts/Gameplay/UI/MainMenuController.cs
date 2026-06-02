using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class MainMenuController : MonoBehaviour
{
    [SerializeField] private string playSceneName = "Level_01";

    private void Awake()
    {
        CreateContinueButton();
    }

    public void Play()
    {
        if (string.IsNullOrWhiteSpace(playSceneName))
        {
            Debug.LogWarning("Play scene name is empty.");
            return;
        }

        ContinueGameProgress.BeginNewGame();
        TutorialHintController.PrepareForNewGame();
        SceneTransition.LoadScene(playSceneName);
    }

    public void ContinueGame()
    {
        if (!ContinueGameProgress.TryGetSavedScene(out var sceneName))
        {
            Debug.LogWarning("There is no saved game to continue.");
            return;
        }

        TutorialHintController.SkipForContinue();
        SceneTransition.LoadScene(sceneName);
    }

    public void Exit()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void CreateContinueButton()
    {
        var playButtonObject = GameObject.Find("PlayButton");
        if (playButtonObject == null || GameObject.Find("ContinueButton") != null)
        {
            return;
        }

        var continueButtonObject = Instantiate(playButtonObject, playButtonObject.transform.parent);
        continueButtonObject.name = "ContinueButton";

        var continueButton = continueButtonObject.GetComponent<Button>();
        continueButton.onClick = new Button.ButtonClickedEvent();
        continueButton.onClick.AddListener(ContinueGame);
        var canContinue = ContinueGameProgress.TryGetSavedScene(out _);
        var colors = continueButton.colors;
        colors.disabledColor = new Color(0f, 0f, 0f, 0.18f);
        continueButton.colors = colors;
        continueButton.interactable = canContinue;

        if (continueButtonObject.TryGetComponent<RectTransform>(out var continueRect))
        {
            continueRect.anchoredPosition = new Vector2(0f, -200f);
        }

        var label = continueButtonObject.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
        {
            label.text = "Continue";

            if (!canContinue)
            {
                label.color = new Color(0.42f, 0.42f, 0.42f, label.color.a);
            }
        }

        var exitButtonObject = GameObject.Find("ExitButton");
        if (exitButtonObject != null && exitButtonObject.TryGetComponent<RectTransform>(out var exitRect))
        {
            exitRect.anchoredPosition = new Vector2(0f, -334f);
        }
    }
}
