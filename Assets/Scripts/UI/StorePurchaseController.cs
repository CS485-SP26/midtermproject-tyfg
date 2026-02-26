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
    private static readonly string[] PurchaseButtonNameHints = { "purchase", "buy", "seed" };
    private static readonly Vector2 TitleStyleButtonSize = new Vector2(160f, 30f);
    private static readonly Vector2 TitleStyleButtonPosition = new Vector2(2f, -77f);

    [Header("Store UI")]
    [SerializeField] private Button purchaseButton;
    [SerializeField] private TMP_Text purchaseButtonText;
    [SerializeField] private bool autoCreatePurchaseButton = true;
    private bool purchaseButtonWasAutoCreated;

    // Registers scene callbacks and ensures controller exists in store scenes.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterSceneBootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        EnsureControllerInScene(SceneManager.GetActiveScene());
    }

    // Static scene callback that validates controller presence by scene.
    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureControllerInScene(scene);
    }

    // Creates/removes runtime controller depending on whether scene is a store scene.
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

    // Initializes economy subscriptions and binds/creates purchase UI.
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

    // Late fallback to auto-bind purchase UI if it appears after Start.
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

    // Cleans subscriptions/listeners when object is destroyed.
    private void OnDestroy()
    {
        if (economyService == null)
            return;

        economyService.ResourceChanged -= HandleResourceChanged;

        if (purchaseButton != null)
            purchaseButton.onClick.RemoveListener(PurchaseSeeds);
    }

    // Public click handler for buying seeds.
    public void PurchaseSeeds()
    {
        TryPurchaseAndNotify();
        UpdatePurchaseAvailability(GetFundsBalance());
    }

    // Economy event callback used to refresh button availability on funds changes.
    private void HandleResourceChanged(EconomyResource resource, int amount)
    {
        if (resource != EconomyResource.Funds)
            return;

        UpdatePurchaseAvailability(amount);
    }

    // Updates purchase button interactable state.
    private void UpdatePurchaseAvailability(int funds)
    {
        if (purchaseButton != null)
            purchaseButton.interactable = true;
    }

    // Resolves existing purchase button or creates one if allowed.
    private void TryAutoBindUI()
    {
        if (purchaseButton != null && !IsUsableStoreButton(purchaseButton))
        {
            purchaseButton = null;
            purchaseButtonText = null;
            purchaseButtonWasAutoCreated = false;
        }

        if (purchaseButton == null)
        {
            purchaseButton = FindExplicitPurchaseButton();
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
        purchaseButton.enabled = true;
        purchaseButton.interactable = true;

        Graphic targetGraphic = purchaseButton.targetGraphic;
        if (targetGraphic != null)
            targetGraphic.raycastTarget = true;

        if (purchaseButtonText == null)
            purchaseButtonText = purchaseButton.GetComponentInChildren<TMP_Text>(true);
    }

    // Finds best existing purchase button candidate in active scene.
    private static Button FindExplicitPurchaseButton()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        Button[] allButtons = FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (Button button in allButtons)
        {
            if (button == null)
                continue;

            if (!BelongsToScene(button.gameObject, activeScene))
                continue;

            if (!HasUsableRaycaster(button))
                continue;

            if (!button.isActiveAndEnabled || !button.gameObject.activeInHierarchy)
                continue;

            string text = GetButtonText(button);
            if (IsLikelyLeaveButton(button))
                continue;

            if (LooksLikePurchaseButton(button, text))
                return button;
        }

        return null;
    }

    // Heuristic: determines if button likely represents purchase action.
    private static bool LooksLikePurchaseButton(Button button, string loweredText)
    {
        if (button == null)
            return false;

        string loweredName = string.IsNullOrWhiteSpace(button.name) ? string.Empty : button.name.ToLowerInvariant();
        foreach (string hint in PurchaseButtonNameHints)
        {
            if (loweredName.Contains(hint) || loweredText.Contains(hint))
                return true;
        }

        int persistentCount = button.onClick.GetPersistentEventCount();
        for (int i = 0; i < persistentCount; i++)
        {
            string method = button.onClick.GetPersistentMethodName(i);
            if (string.IsNullOrWhiteSpace(method))
                continue;

            string loweredMethod = method.ToLowerInvariant();
            foreach (string hint in PurchaseButtonNameHints)
            {
                if (loweredMethod.Contains(hint))
                    return true;
            }
        }

        return false;
    }

    // Extracts lowercase button text from TMP/Text/name for matching.
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

    // Creates a default styled purchase button when scene lacks one.
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

    // Returns true if a button is valid for active store scene interaction.
    private static bool IsUsableStoreButton(Button button)
    {
        if (button == null || button.gameObject == null)
            return false;

        Scene activeScene = SceneManager.GetActiveScene();
        if (!BelongsToScene(button.gameObject, activeScene))
            return false;

        return HasUsableRaycaster(button);
    }

    // Checks whether GameObject belongs to specific scene instance.
    private static bool BelongsToScene(GameObject gameObject, Scene scene)
    {
        if (gameObject == null || !scene.IsValid())
            return false;

        return gameObject.scene.IsValid() && gameObject.scene.handle == scene.handle;
    }

    // Validates that button has an active parent canvas with GraphicRaycaster.
    private static bool HasUsableRaycaster(Button button)
    {
        if (button == null)
            return false;

        Canvas parentCanvas = button.GetComponentInParent<Canvas>(true);
        if (parentCanvas == null || !parentCanvas.isActiveAndEnabled)
            return false;

        GraphicRaycaster raycaster = parentCanvas.GetComponent<GraphicRaycaster>();
        return raycaster != null && raycaster.isActiveAndEnabled;
    }

    // Creates button label and copies text style from reference button when possible.
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

    // Copies transition/graphic style from reference button to generated button.
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

    // Finds a button to use as style reference (prefer leave button).
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

    // Heuristic to detect leave/back/exit scene buttons.
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

    // Returns first button that looks like a leave/back control.
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

    // Resolves usable scene canvas with raycaster for generated button placement.
    private static Canvas ResolveCanvas()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid())
        {
            GameObject[] roots = activeScene.GetRootGameObjects();
            foreach (GameObject root in roots)
            {
                if (root == null)
                    continue;

                Canvas[] canvasesInScene = root.GetComponentsInChildren<Canvas>(true);
                foreach (Canvas canvas in canvasesInScene)
                {
                    if (canvas == null || !canvas.isActiveAndEnabled)
                        continue;

                    GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
                    if (raycaster != null && raycaster.isActiveAndEnabled)
                        return canvas;
                }
            }
        }

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            if (canvas == null || !canvas.isActiveAndEnabled)
                continue;

            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null && raycaster.isActiveAndEnabled)
                return canvas;
        }

        return null;
    }

    // Determines if a scene is considered a store scene by name.
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

    // Destroys auto-created purchase button outside store context.
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

    // Removes leaked auto-created purchase buttons from non-store scenes.
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
