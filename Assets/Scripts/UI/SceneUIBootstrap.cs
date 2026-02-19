using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;

public static class SceneUIBootstrap
{
    private const string IntroSceneName = "Scene0-Intro";
    private const string FarmSceneName = "Scene1-FarmingSim";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        ApplySceneFixes(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplySceneFixes(scene);
    }

    private static void ApplySceneFixes(Scene scene)
    {
        EnsureValidUIInputActions();

        if (scene.name == IntroSceneName)
            RebindIntroStartButton();
    }

    private static void EnsureValidUIInputActions()
    {
        EventSystem[] eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        foreach (EventSystem eventSystem in eventSystems)
        {
            if (eventSystem == null)
                continue;

            InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
                continue;

            // Defensive fix for broken/missing serialized UI input action references.
            inputModule.AssignDefaultActions();
        }
    }

    private static void RebindIntroStartButton()
    {
        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (Button button in buttons)
        {
            if (!IsLikelyStartButton(button))
                continue;

            button.onClick.RemoveListener(LoadFarmScene);
            button.onClick.AddListener(LoadFarmScene);
        }
    }

    private static bool IsLikelyStartButton(Button button)
    {
        if (button == null)
            return false;

        string buttonName = button.name.ToLowerInvariant();
        if (buttonName.Contains("start"))
            return true;

        TMP_Text tmpText = button.GetComponentInChildren<TMP_Text>(true);
        if (tmpText != null && !string.IsNullOrWhiteSpace(tmpText.text))
            return tmpText.text.ToLowerInvariant().Contains("start");

        Text legacyText = button.GetComponentInChildren<Text>(true);
        if (legacyText != null && !string.IsNullOrWhiteSpace(legacyText.text))
            return legacyText.text.ToLowerInvariant().Contains("start");

        return false;
    }

    private static void LoadFarmScene()
    {
        SceneManager.LoadScene(FarmSceneName);
    }
}
