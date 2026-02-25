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
    private const string PersistentHudRootName = "PersistentGameplayHUD";
    private const string IntroTitleObjectName = "GameTitle";
    private const string IntroStartButtonObjectName = "Start";
    private const string IntroStartLabelObjectName = "StartText";
    private const string IntroTitleText = "Farming Game";
    private const string IntroStartText = "Start";
    private static readonly string[] PersistentHudTextNames = { "DayLabel", "FundAmount", "SeedAmount", "seeds" };
    private static readonly Vector2 IntroTitleSize = new Vector2(400f, 50f);
    private static readonly Vector2 IntroStartButtonSize = new Vector2(160f, 30f);
    private static readonly Vector2 IntroStartButtonPosition = new Vector2(2f, -77f);
    private static bool startupSceneValidated;
    private static GameObject persistentHudRoot;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        startupSceneValidated = false;
        persistentHudRoot = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;

        Scene activeScene = SceneManager.GetActiveScene();
        if (ShouldRedirectToIntroScene(activeScene))
        {
            SceneManager.LoadScene(IntroSceneName);
            return;
        }

        ApplySceneFixes(activeScene);
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplySceneFixes(scene);
    }

    private static void ApplySceneFixes(Scene scene)
    {
        EnsureValidUIInputActions();
        CleanupSceneSpecificUI(scene);
        EnsurePersistentGameplayHud(scene);

        if (scene.name == IntroSceneName)
        {
            EnsureIntroOverlay();
            RebindIntroStartButton();
        }

        if (scene.name == StoreSceneName)
            EnsureStorePurchaseController();
    }

    private static bool ShouldRedirectToIntroScene(Scene activeScene)
    {
        if (startupSceneValidated)
            return false;

        startupSceneValidated = true;
        if (!activeScene.IsValid())
            return false;

        if (string.IsNullOrWhiteSpace(activeScene.name))
            return false;

        return !string.Equals(activeScene.name, IntroSceneName, StringComparison.Ordinal);
    }

    private static void EnsurePersistentGameplayHud(Scene scene)
    {
        if (scene.name == IntroSceneName)
        {
            SetPersistentHudVisible(false);
            return;
        }

        EnsurePersistentHudRoot();
        if (persistentHudRoot == null)
            return;

        PromoteHudObjectsFromScene(scene);
        CleanupDuplicateHudObjectsInScene(scene);
        SetPersistentHudVisible(true);
    }

    private static void EnsurePersistentHudRoot()
    {
        if (persistentHudRoot != null)
            return;

        GameObject root = new GameObject(PersistentHudRootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800f, 600f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;

        Object.DontDestroyOnLoad(root);
        persistentHudRoot = root;
    }

    private static void PromoteHudObjectsFromScene(Scene scene)
    {
        if (persistentHudRoot == null || !scene.IsValid())
            return;

        GameObject[] roots = scene.GetRootGameObjects();
        bool hasPersistentBars = persistentHudRoot.GetComponentsInChildren<ProgressBar>(true).Length > 0;

        if (!hasPersistentBars)
        {
            foreach (GameObject root in roots)
            {
                if (root == null)
                    continue;

                ProgressBar[] bars = root.GetComponentsInChildren<ProgressBar>(true);
                foreach (ProgressBar bar in bars)
                {
                    if (bar != null && bar.gameObject != null)
                        PromoteHudObject(bar.gameObject);
                }
            }
        }

        foreach (string textName in PersistentHudTextNames)
        {
            if (PersistentHudHasTextNamed(textName))
                continue;

            TMP_Text sourceText = FindSceneHudTextByName(scene, textName);
            if (sourceText != null && sourceText.gameObject != null)
                PromoteHudObject(sourceText.gameObject);
        }
    }

    private static TMP_Text FindSceneHudTextByName(Scene scene, string textName)
    {
        if (!scene.IsValid() || string.IsNullOrWhiteSpace(textName))
            return null;

        GameObject[] roots = scene.GetRootGameObjects();
        foreach (GameObject root in roots)
        {
            if (root == null)
                continue;

            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text text in texts)
            {
                if (text != null && string.Equals(text.name, textName, StringComparison.Ordinal))
                    return text;
            }
        }

        return null;
    }

    private static void PromoteHudObject(GameObject hudObject)
    {
        if (hudObject == null || persistentHudRoot == null)
            return;

        if (hudObject == persistentHudRoot)
            return;

        Transform hudTransform = hudObject.transform;
        if (hudTransform != null && hudTransform.IsChildOf(persistentHudRoot.transform))
            return;

        hudObject.transform.SetParent(persistentHudRoot.transform, false);
    }

    private static bool PersistentHudHasTextNamed(string textName)
    {
        if (persistentHudRoot == null || string.IsNullOrWhiteSpace(textName))
            return false;

        TMP_Text[] texts = persistentHudRoot.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            if (text != null && string.Equals(text.name, textName, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static void CleanupDuplicateHudObjectsInScene(Scene scene)
    {
        if (persistentHudRoot == null || !scene.IsValid())
            return;

        GameObject[] roots = scene.GetRootGameObjects();

        bool hasPersistentBars = persistentHudRoot.GetComponentsInChildren<ProgressBar>(true).Length > 0;
        if (hasPersistentBars)
        {
            foreach (GameObject root in roots)
            {
                if (root == null)
                    continue;

                ProgressBar[] bars = root.GetComponentsInChildren<ProgressBar>(true);
                foreach (ProgressBar bar in bars)
                {
                    if (bar != null && bar.gameObject != null)
                        Object.Destroy(bar.gameObject);
                }
            }
        }

        foreach (string textName in PersistentHudTextNames)
        {
            if (!PersistentHudHasTextNamed(textName))
                continue;

            foreach (GameObject root in roots)
            {
                if (root == null)
                    continue;

                TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
                foreach (TMP_Text text in texts)
                {
                    if (text == null || text.gameObject == null)
                        continue;

                    if (!string.Equals(text.name, textName, StringComparison.Ordinal))
                        continue;

                    Object.Destroy(text.gameObject);
                }
            }
        }
    }

    private static void SetPersistentHudVisible(bool visible)
    {
        if (persistentHudRoot == null)
            return;

        if (persistentHudRoot.activeSelf != visible)
            persistentHudRoot.SetActive(visible);
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
            bool hasPersistentSceneLoad = HasPersistentMethod(button, "LoadScenebyName") ||
                                          HasPersistentMethod(button, "LoadSceneByName") ||
                                          HasPersistentMethod(button, "LoadFarmScene");
            if (!hasPersistentSceneLoad)
                button.onClick.AddListener(LoadFarmScene);
        }
    }

    private static void EnsureIntroOverlay()
    {
        Canvas canvas = ResolveCanvas();
        if (canvas == null)
            return;

        EnsureEventSystemExists();

        if (FindIntroTitle() == null)
            CreateIntroTitle(canvas.transform);

        Button startButton = FindIntroStartButton();
        if (startButton == null)
            startButton = CreateIntroStartButton(canvas.transform);

        if (startButton == null)
            return;

        EnsureStartButtonLabel(startButton);
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

    private static TMP_Text FindIntroTitle()
    {
        TMP_Text[] labels = Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
        foreach (TMP_Text label in labels)
        {
            if (label == null)
                continue;

            string textValue = string.IsNullOrWhiteSpace(label.text) ? string.Empty : label.text.Trim();
            if (string.Equals(label.name, IntroTitleObjectName, StringComparison.Ordinal) ||
                string.Equals(textValue, IntroTitleText, StringComparison.OrdinalIgnoreCase))
                return label;
        }

        return null;
    }

    private static Button FindIntroStartButton()
    {
        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (Button button in buttons)
        {
            if (button == null)
                continue;

            if (string.Equals(button.name, IntroStartButtonObjectName, StringComparison.Ordinal) || IsLikelyStartButton(button))
                return button;
        }

        return null;
    }

    private static TMP_Text CreateIntroTitle(Transform parent)
    {
        if (parent == null)
            return null;

        GameObject titleGo = new GameObject(IntroTitleObjectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(parent, false);

        TextMeshProUGUI titleText = titleGo.GetComponent<TextMeshProUGUI>();
        titleText.text = IntroTitleText;
        titleText.fontSize = 36f;
        titleText.color = new Color(0.645283f, 0.34931934f, 0.11566386f, 1f);
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.raycastTarget = false;
        titleText.textWrappingMode = TextWrappingModes.NoWrap;

        RectTransform rect = titleText.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = IntroTitleSize;

        return titleText;
    }

    private static Button CreateIntroStartButton(Transform parent)
    {
        if (parent == null)
            return null;

        GameObject buttonGo = new GameObject(IntroStartButtonObjectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonGo.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = IntroStartButtonPosition;
        buttonRect.sizeDelta = IntroStartButtonSize;

        Image image = buttonGo.GetComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = true;

        return buttonGo.GetComponent<Button>();
    }

    private static void EnsureStartButtonLabel(Button startButton)
    {
        if (startButton == null)
            return;

        TMP_Text label = startButton.GetComponentInChildren<TMP_Text>(true);
        if (label == null)
        {
            GameObject labelGo = new GameObject(IntroStartLabelObjectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(startButton.transform, false);

            RectTransform labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = labelGo.GetComponent<TextMeshProUGUI>();
            tmp.text = IntroStartText;
            tmp.fontSize = 24f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            return;
        }

        label.text = IntroStartText;
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

    private static Canvas ResolveCanvas()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            if (canvas != null && canvas.isActiveAndEnabled)
                return canvas;
        }

        GameObject canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas created = canvasGo.GetComponent<Canvas>();
        created.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return created;
    }

    private static void EnsureEventSystemExists()
    {
        EventSystem[] eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        foreach (EventSystem eventSystem in eventSystems)
        {
            if (eventSystem != null && eventSystem.isActiveAndEnabled)
                return;
        }

        GameObject eventSystemGo = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        InputSystemUIInputModule inputModule = eventSystemGo.GetComponent<InputSystemUIInputModule>();
        if (inputModule != null)
            inputModule.AssignDefaultActions();
    }
}
