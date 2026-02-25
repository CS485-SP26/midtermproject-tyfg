using System;
using Core;
using Farming;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StorePurchaseController : SeedPurchaseControllerBase
{
    private const string StoreSceneName = "Scene2-Store";
    private const string AutoCreatedButtonName = "PurchaseSeedsButton";
    private static readonly Vector2 TitleStyleButtonSize = new Vector2(160f, 30f);
    private static readonly Vector2 TitleStyleButtonPosition = new Vector2(2f, -77f);

    [Header("Store UI")]
    [SerializeField] private Button purchaseButton;
    [SerializeField] private TMP_Text purchaseButtonText;
    [SerializeField] private bool autoCreatePurchaseButton = true;
    private bool purchaseButtonWasAutoCreated;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterSceneBootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        EnsureControllerInScene(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureControllerInScene(scene);
    }

    private static void EnsureControllerInScene(Scene scene)
    {
        if (!IsStoreScene(scene))
        {
            CleanupAutoCreatedButtonsInNonStoreScene();
            return;
        }

        StorePurchaseController[] existing = FindObjectsByType<StorePurchaseController>(FindObjectsSortMode.None);
        if (existing.Length > 0)
            return;

        GameObject go = new GameObject(nameof(StorePurchaseController));
        go.AddComponent<StorePurchaseController>();
    }

    private void Start()
    {
        economyService = GameManager.Instance;
        if (economyService != null)
            economyService.ResourceChanged += HandleResourceChanged;

        if (!IsStoreScene(SceneManager.GetActiveScene()))
        {
            CleanupAutoCreatedButton();
            return;
        }

        TryAutoBindUI();

        if (purchaseButtonText != null)
            purchaseButtonText.SetText("Purchase Seeds ({0})", seedCost);

        UpdatePurchaseAvailability(GetFundsBalance());
    }

    private void Update()
    {
        if (!IsStoreScene(SceneManager.GetActiveScene()))
        {
            CleanupAutoCreatedButton();
            return;
        }

        if (purchaseButton != null || !autoCreatePurchaseButton)
            return;

        TryAutoBindUI();
        if (purchaseButtonText != null)
            purchaseButtonText.SetText("Purchase Seeds ({0})", seedCost);

        UpdatePurchaseAvailability(GetFundsBalance());
    }

    private void OnDestroy()
    {
        if (economyService == null)
            return;

        economyService.ResourceChanged -= HandleResourceChanged;

        if (purchaseButton != null)
            purchaseButton.onClick.RemoveListener(PurchaseSeeds);
    }

    public void PurchaseSeeds()
    {
        TryPurchaseAndNotify();
        UpdatePurchaseAvailability(GetFundsBalance());
    }

    private void HandleResourceChanged(EconomyResource resource, int amount)
    {
        if (resource != EconomyResource.Funds)
            return;

        UpdatePurchaseAvailability(amount);
    }

    private void UpdatePurchaseAvailability(int funds)
    {
        if (purchaseButton != null)
            purchaseButton.interactable = true;
    }

    private void TryAutoBindUI()
    {
        if (purchaseButton == null)
        {
            purchaseButton = FindLikelyPurchaseButton();
            purchaseButtonWasAutoCreated = false;
        }

        if (purchaseButton != null && IsLikelyLeaveButton(purchaseButton))
            purchaseButton = null;

        if (purchaseButton == null && autoCreatePurchaseButton)
            purchaseButton = CreatePurchaseButton();

        if (purchaseButton == null)
            return;

        purchaseButton.onClick.RemoveListener(PurchaseSeeds);
        purchaseButton.onClick.AddListener(PurchaseSeeds);

        if (purchaseButtonText == null)
            purchaseButtonText = purchaseButton.GetComponentInChildren<TMP_Text>(true);
    }

    private static Button FindLikelyPurchaseButton()
    {
        Button[] allButtons = FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (Button button in allButtons)
        {
            if (button == null)
                continue;

            string text = GetButtonText(button);
            if (IsLikelyLeaveButton(button))
                continue;

            if (text.Contains("buy") || text.Contains("purchase") || text.Contains("seed"))
                return button;
        }

        // Safe fallback: only use buttons that have no persistent click handlers.
        foreach (Button button in allButtons)
        {
            if (button == null)
                continue;

            if (IsLikelyLeaveButton(button))
                continue;

            if (button.onClick.GetPersistentEventCount() == 0)
                return button;
        }

        return null;
    }

    private static string GetButtonText(Button button)
    {
        if (button == null)
            return string.Empty;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null && !string.IsNullOrWhiteSpace(label.text))
            return label.text.ToLowerInvariant();

        Text legacyText = button.GetComponentInChildren<Text>(true);
        if (legacyText != null && !string.IsNullOrWhiteSpace(legacyText.text))
            return legacyText.text.ToLowerInvariant();

        return button.name.ToLowerInvariant();
    }

    private Button CreatePurchaseButton()
    {
        Canvas canvas = ResolveCanvas();
        if (canvas == null)
            return null;

        GameObject buttonGo = new GameObject(AutoCreatedButtonName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(canvas.transform, false);

        RectTransform buttonRect = buttonGo.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = TitleStyleButtonPosition;
        buttonRect.sizeDelta = TitleStyleButtonSize;

        Image buttonImage = buttonGo.GetComponent<Image>();
        Button button = buttonGo.GetComponent<Button>();
        ApplyStyleFromReferenceButton(button, buttonImage);
        purchaseButtonText = CreateButtonLabel(buttonGo.transform, FindStyleReferenceButton());
        purchaseButtonWasAutoCreated = true;
        return button;
    }

    private static TMP_Text CreateButtonLabel(Transform parent, Button styleSource)
    {
        if (parent == null)
            return null;

        GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(parent, false);

        RectTransform labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelGo.GetComponent<TextMeshProUGUI>();
        TMP_Text sourceText = styleSource != null ? styleSource.GetComponentInChildren<TMP_Text>(true) : null;
        if (sourceText != null)
        {
            label.font = sourceText.font;
            label.fontSharedMaterial = sourceText.fontSharedMaterial;
            label.fontSize = sourceText.fontSize;
            label.color = sourceText.color;
            label.alignment = sourceText.alignment;
            label.textWrappingMode = sourceText.textWrappingMode;
        }
        else
        {
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 24f;
            label.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            label.textWrappingMode = TextWrappingModes.NoWrap;
        }

        return label;
    }

    private static void ApplyStyleFromReferenceButton(Button targetButton, Image targetImage)
    {
        if (targetButton == null || targetImage == null)
            return;

        Button sourceButton = FindStyleReferenceButton();
        if (sourceButton == null)
        {
            targetImage.color = Color.white;

            ColorBlock defaultColors = targetButton.colors;
            defaultColors.normalColor = Color.white;
            defaultColors.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
            defaultColors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            defaultColors.selectedColor = defaultColors.highlightedColor;
            defaultColors.disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.5f);
            targetButton.colors = defaultColors;
            targetButton.targetGraphic = targetImage;
            return;
        }

        targetButton.transition = sourceButton.transition;
        targetButton.colors = sourceButton.colors;
        targetButton.spriteState = sourceButton.spriteState;
        targetButton.animationTriggers = sourceButton.animationTriggers;

        Image sourceImage = sourceButton.targetGraphic as Image;
        if (sourceImage != null)
        {
            targetImage.sprite = sourceImage.sprite;
            targetImage.type = sourceImage.type;
            targetImage.material = sourceImage.material;
            targetImage.color = sourceImage.color;
        }
        else
        {
            targetImage.color = Color.white;
        }

        targetButton.targetGraphic = targetImage;
    }

    private static Button FindStyleReferenceButton()
    {
        Button leaveButton = FindLeaveButton();
        if (leaveButton != null)
            return leaveButton;

        Button[] allButtons = FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (Button button in allButtons)
        {
            if (button != null)
                return button;
        }

        return null;
    }

    private static bool IsLikelyLeaveButton(Button button)
    {
        if (button == null)
            return false;

        string text = GetButtonText(button);
        if (text.Contains("leave") || text.Contains("back") || text.Contains("exit"))
            return true;

        int persistentCount = button.onClick.GetPersistentEventCount();
        for (int i = 0; i < persistentCount; i++)
        {
            string method = button.onClick.GetPersistentMethodName(i);
            if (string.IsNullOrWhiteSpace(method))
                continue;

            string loweredMethod = method.ToLowerInvariant();
            if (loweredMethod.Contains("leave") ||
                loweredMethod.Contains("back") ||
                loweredMethod.Contains("exit") ||
                loweredMethod.Contains("loadscene"))
                return true;
        }

        return false;
    }

    private static Button FindLeaveButton()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (Button button in buttons)
        {
            if (IsLikelyLeaveButton(button))
                return button;
        }

        return null;
    }

    private static Canvas ResolveCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            if (canvas != null && canvas.isActiveAndEnabled)
                return canvas;
        }

        return null;
    }

    private static bool IsStoreScene(Scene scene)
    {
        if (!scene.IsValid())
            return false;

        string sceneName = scene.name;
        if (string.IsNullOrWhiteSpace(sceneName))
            return false;

        if (string.Equals(sceneName, StoreSceneName, StringComparison.Ordinal))
            return true;

        return sceneName.IndexOf("store", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void CleanupAutoCreatedButton()
    {
        if (purchaseButton == null)
            return;

        if (!purchaseButtonWasAutoCreated && !string.Equals(purchaseButton.name, AutoCreatedButtonName, StringComparison.Ordinal))
            return;

        if (purchaseButtonText != null)
            purchaseButtonText = null;

        UnityEngine.Object.Destroy(purchaseButton.gameObject);
        purchaseButton = null;
        purchaseButtonWasAutoCreated = false;
    }

    private static void CleanupAutoCreatedButtonsInNonStoreScene()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (Button button in buttons)
        {
            if (button == null)
                continue;

            if (!string.Equals(button.name, AutoCreatedButtonName, StringComparison.Ordinal))
                continue;

            UnityEngine.Object.Destroy(button.gameObject);
        }
    }
}
