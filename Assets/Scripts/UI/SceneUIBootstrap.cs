using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

public static class SceneUIBootstrap
{
    private const string IntroSceneName = "Scene0-Intro";
    private const string FarmSceneName = "Scene1-FarmingSim";
    private const string StoreSceneName = "Scene2-Store";
    private const string AutoCreatedPurchaseButtonName = "PurchaseSeedsButton";

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
        CleanupSceneSpecificUI(scene);

        if (scene.name == IntroSceneName)
            RebindIntroStartButton();

        if (scene.name == StoreSceneName)
            EnsureStorePurchaseController();
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

    private static void EnsureStorePurchaseController()
    {
        StorePurchaseController[] controllers = Object.FindObjectsByType<StorePurchaseController>(FindObjectsSortMode.None);
        if (controllers != null && controllers.Length > 0)
            return;

        GameObject go = new GameObject(nameof(StorePurchaseController));
        go.AddComponent<StorePurchaseController>();
    }

    private static void CleanupSceneSpecificUI(Scene scene)
    {
        if (scene.name != IntroSceneName)
            CleanupIntroOnlyUI();

        if (scene.name != StoreSceneName)
            CleanupStoreOnlyUI();
    }

    private static void CleanupIntroOnlyUI()
    {
        TMP_Text[] labels = Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
        foreach (TMP_Text label in labels)
        {
            if (label == null || label.gameObject == null)
                continue;

            string textValue = string.IsNullOrWhiteSpace(label.text) ? string.Empty : label.text.Trim();
            bool looksLikeIntroTitle = string.Equals(label.name, "GameTitle", StringComparison.Ordinal) ||
                                       string.Equals(textValue, "Farming Game", StringComparison.OrdinalIgnoreCase);
            bool looksLikeIntroStartLabel = string.Equals(label.name, "StartText", StringComparison.Ordinal) ||
                                            string.Equals(textValue, "Start", StringComparison.OrdinalIgnoreCase);

            if (looksLikeIntroTitle)
            {
                Object.Destroy(label.gameObject);
                continue;
            }

            if (!looksLikeIntroStartLabel)
                continue;

            Button startButton = label.GetComponentInParent<Button>();
            if (startButton != null && startButton.gameObject != null)
                Object.Destroy(startButton.gameObject);
            else
                Object.Destroy(label.gameObject);
        }
    }

    private static void CleanupStoreOnlyUI()
    {
        StorePurchaseController[] controllers = Object.FindObjectsByType<StorePurchaseController>(FindObjectsSortMode.None);
        foreach (StorePurchaseController controller in controllers)
        {
            if (controller != null && controller.gameObject != null)
                Object.Destroy(controller.gameObject);
        }

        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (Button button in buttons)
        {
            if (button == null || button.gameObject == null)
                continue;

            if (string.Equals(button.name, AutoCreatedPurchaseButtonName, StringComparison.Ordinal))
            {
                Object.Destroy(button.gameObject);
                continue;
            }

            string buttonText = GetButtonText(button);
            bool looksLikeLeaveStoreButton = buttonText.Contains("leave store") || HasPersistentMethod(button, "LeaveStore");
            if (looksLikeLeaveStoreButton)
                Object.Destroy(button.gameObject);
        }
    }

    private static bool HasPersistentMethod(Button button, string methodName)
    {
        if (button == null || string.IsNullOrWhiteSpace(methodName))
            return false;

        int callCount = button.onClick.GetPersistentEventCount();
        for (int i = 0; i < callCount; i++)
        {
            string persistentMethod = button.onClick.GetPersistentMethodName(i);
            if (string.Equals(persistentMethod, methodName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string GetButtonText(Button button)
    {
        if (button == null)
            return string.Empty;

        TMP_Text tmpText = button.GetComponentInChildren<TMP_Text>(true);
        if (tmpText != null && !string.IsNullOrWhiteSpace(tmpText.text))
            return tmpText.text.ToLowerInvariant();

        Text legacyText = button.GetComponentInChildren<Text>(true);
        if (legacyText != null && !string.IsNullOrWhiteSpace(legacyText.text))
            return legacyText.text.ToLowerInvariant();

        return string.IsNullOrWhiteSpace(button.name) ? string.Empty : button.name.ToLowerInvariant();
    }
}
