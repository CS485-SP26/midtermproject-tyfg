using Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class CurrencyTextUI : MonoBehaviour
{
    private const string IntroSceneName = "Scene0-Intro";
    private static CurrencyTextUI instance;

    [Header("Label Targets")]
    [SerializeField] private TMP_Text fundsText;
    [SerializeField] private TMP_Text seedsText;

    [Header("Labels")]
    [SerializeField] private string fundsLabel = "Funds: $";
    [SerializeField] private string seedsLabel = "Seeds:";

    [Header("Auto-Bind Names")]
    [SerializeField] private string fundsObjectName = "FundAmount";
    [SerializeField] private string seedsObjectName = "SeedAmount";
    [SerializeField] private float seedLabelVerticalSpacing = 6f;
    [SerializeField] private Vector2 fallbackFundsAnchoredPosition = new Vector2(-28f, -22f);
    [SerializeField] private Vector2 fallbackLabelSize = new Vector2(220f, 44f);
    [SerializeField] private bool allowAutoCreatedFundsFallback = true;

    [Header("Unified HUD Style")]
    [SerializeField] private bool enforceUnifiedHudStyle = true;
    [SerializeField] private float unifiedHudFontSize = 28f;
    [SerializeField] private Color unifiedHudTextColor = Color.white;
    [SerializeField] private TextAlignmentOptions unifiedHudAlignment = TextAlignmentOptions.TopRight;

    private GameManager gameManager;
    private int lastRenderedFunds = int.MinValue;
    private int lastRenderedSeeds = int.MinValue;
    private bool fundsTextWasAutoCreated;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterSceneBootstrap()
    {
        SceneManager.sceneLoaded -= HandleStaticSceneLoaded;
        SceneManager.sceneLoaded += HandleStaticSceneLoaded;
        EnsureInstanceInActiveScene();
    }

    private static void HandleStaticSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureInstanceInActiveScene();
    }

    private static void EnsureInstanceInActiveScene()
    {
        if (FindObjectsByType<CurrencyTextUI>(FindObjectsSortMode.None).Length > 0)
            return;

        GameObject go = new GameObject("CurrencyTextUI");
        go.AddComponent<CurrencyTextUI>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            // Keep scene UI objects intact; only remove duplicate manager components.
            Destroy(this);
            return;
        }

        instance = this;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        RefreshForCurrentScene();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;

        if (gameManager == null)
            return;

        gameManager.FundsChanged -= HandleFundsChanged;
        gameManager.SeedsChanged -= HandleSeedsChanged;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsIntroScene(scene))
        {
            HideHudForIntroScene();
            return;
        }

        BindGameManager();
        AutoBindTextTargets();
        ApplyUnifiedHudStyle();
        RefreshAll();
    }

    private void Update()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (IsIntroScene(activeScene))
        {
            HideHudForIntroScene();
            return;
        }

        EnsureHudVisible();

        if (gameManager == null)
            BindGameManager();

        if (gameManager == null)
            return;

        if (gameManager.Funds != lastRenderedFunds)
            UpdateFundsText(gameManager.Funds);

        if (gameManager.Seeds != lastRenderedSeeds)
            UpdateSeedsText(gameManager.Seeds);
    }

    private void HandleFundsChanged(int funds)
    {
        UpdateFundsText(funds);
    }

    private void HandleSeedsChanged(int seeds)
    {
        UpdateSeedsText(seeds);
    }

    private void RefreshAll()
    {
        if (gameManager == null)
            return;

        UpdateFundsText(gameManager.Funds);
        UpdateSeedsText(gameManager.Seeds);
    }

    private void UpdateFundsText(int funds)
    {
        if (fundsText == null)
            AutoBindTextTargets();

        if (fundsText != null)
            fundsText.text = fundsLabel.EndsWith("$") ? $"{fundsLabel}{funds}" : $"{fundsLabel} {funds}";

        lastRenderedFunds = funds;
    }

    /*
    * This feels like it should be refactored.
    */
    private void UpdateSeedsText(int seeds)
    {
        if (seedsText == null)
            AutoBindTextTargets();

        if (seedsText != null)
            seedsText.text = $"{seedsLabel} {seeds}";

        lastRenderedSeeds = seeds;
    }

    private void AutoBindTextTargets()
    {
        if (IsIntroScene(SceneManager.GetActiveScene()))
            return;

        CleanupLegacyAutoFundsTexts();

        TMP_Text namedFunds = FindTextByName(fundsObjectName);
        TMP_Text fundsLike = FindFundsLikeText();
        TMP_Text preferredFunds = namedFunds != null ? namedFunds : fundsLike;

        if (preferredFunds != null && preferredFunds != fundsText)
        {
            if (fundsTextWasAutoCreated && fundsText != null && fundsText.gameObject != null)
                Destroy(fundsText.gameObject);

            fundsText = preferredFunds;
            fundsTextWasAutoCreated = false;
        }

        if (fundsText == null && allowAutoCreatedFundsFallback)
            fundsText = CreateFundsTextFallback();

        if (seedsText == null)
            seedsText = FindTextByName(seedsObjectName);

        if (seedsText == null)
            seedsText = CreateSeedsTextBelowFunds();

        AlignSeedsTextBelowFunds();
    }

    private TMP_Text CreateSeedsTextBelowFunds()
    {
        if (fundsText == null || string.IsNullOrWhiteSpace(seedsObjectName))
            return null;

        TMP_Text existing = FindTextByName(seedsObjectName);
        if (existing != null)
            return existing;

        TextMeshProUGUI fundsLabel = fundsText as TextMeshProUGUI;
        if (fundsLabel == null)
            return null;

        RectTransform fundsRect = fundsLabel.rectTransform;
        if (fundsRect == null || fundsRect.parent == null)
            return null;

        GameObject go = new GameObject(seedsObjectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(fundsRect.parent, false);

        TextMeshProUGUI seedsLabelText = go.GetComponent<TextMeshProUGUI>();
        seedsLabelText.font = fundsLabel.font;
        seedsLabelText.fontSharedMaterial = fundsLabel.fontSharedMaterial;
        seedsLabelText.fontSize = fundsLabel.fontSize;
        seedsLabelText.color = fundsLabel.color;
        seedsLabelText.alignment = fundsLabel.alignment;
        seedsLabelText.raycastTarget = fundsLabel.raycastTarget;
        seedsLabelText.textWrappingMode = TextWrappingModes.NoWrap;
        seedsLabelText.text = $"{seedsLabel} 0";

        RectTransform seedsRect = seedsLabelText.rectTransform;
        seedsRect.anchorMin = fundsRect.anchorMin;
        seedsRect.anchorMax = fundsRect.anchorMax;
        seedsRect.pivot = fundsRect.pivot;
        seedsRect.sizeDelta = fundsRect.sizeDelta;

        float lineHeight = fundsRect.sizeDelta.y > 1f ? fundsRect.sizeDelta.y : (fundsLabel.fontSize + 8f);
        float spacing = Mathf.Max(seedLabelVerticalSpacing, 12f);
        seedsRect.anchoredPosition = fundsRect.anchoredPosition + new Vector2(0f, -(lineHeight + spacing));

        return seedsLabelText;
    }

    private TMP_Text CreateFundsTextFallback()
    {
        if (string.IsNullOrWhiteSpace(fundsObjectName))
            fundsObjectName = "FundAmount";

        Canvas canvas = ResolveCanvas();
        if (canvas == null)
            return null;

        GameObject go = new GameObject($"{fundsObjectName}_Auto", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(canvas.transform, false);

        TextMeshProUGUI fundsLabelText = go.GetComponent<TextMeshProUGUI>();
        fundsLabelText.text = $"{fundsLabel}0";
        fundsLabelText.fontSize = 28f;
        fundsLabelText.alignment = TextAlignmentOptions.TopRight;
        fundsLabelText.color = Color.white;
        fundsLabelText.raycastTarget = false;
        fundsLabelText.textWrappingMode = TextWrappingModes.NoWrap;

        RectTransform rt = fundsLabelText.rectTransform;
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = fallbackFundsAnchoredPosition;
        rt.sizeDelta = fallbackLabelSize;

        fundsTextWasAutoCreated = true;
        return fundsLabelText;
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

    private void CleanupLegacyAutoFundsTexts()
    {
        string autoName = string.IsNullOrWhiteSpace(fundsObjectName) ? "FundAmount_Auto" : $"{fundsObjectName}_Auto";
        TMP_Text[] allTexts = FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
        foreach (TMP_Text text in allTexts)
        {
            if (text == null || text == fundsText || text.gameObject == null)
                continue;

            if (text.name == autoName)
                Destroy(text.gameObject);
        }
    }

    private void AlignSeedsTextBelowFunds()
    {
        TextMeshProUGUI fundsLabel = fundsText as TextMeshProUGUI;
        TextMeshProUGUI seedsLabel = seedsText as TextMeshProUGUI;
        if (fundsLabel == null || seedsLabel == null)
            return;

        RectTransform fundsRect = fundsLabel.rectTransform;
        RectTransform seedsRect = seedsLabel.rectTransform;
        if (fundsRect == null || seedsRect == null)
            return;

        if (fundsRect.parent != null && seedsRect.parent != fundsRect.parent)
            seedsRect.SetParent(fundsRect.parent, false);

        seedsRect.anchorMin = fundsRect.anchorMin;
        seedsRect.anchorMax = fundsRect.anchorMax;
        seedsRect.pivot = fundsRect.pivot;
        seedsRect.sizeDelta = fundsRect.sizeDelta;

        float lineHeight = fundsRect.sizeDelta.y > 1f ? fundsRect.sizeDelta.y : (fundsLabel.fontSize + 8f);
        float spacing = Mathf.Max(seedLabelVerticalSpacing, 12f);
        seedsRect.anchoredPosition = fundsRect.anchoredPosition + new Vector2(0f, -(lineHeight + spacing));

        seedsLabel.alignment = fundsLabel.alignment;
        seedsLabel.font = fundsLabel.font;
        seedsLabel.fontSharedMaterial = fundsLabel.fontSharedMaterial;
        seedsLabel.fontSize = fundsLabel.fontSize;
        seedsLabel.color = fundsLabel.color;
        seedsLabel.raycastTarget = fundsLabel.raycastTarget;
    }

    private static TMP_Text FindTextByName(string targetName)
    {
        if (string.IsNullOrWhiteSpace(targetName))
            return null;

        TMP_Text[] allTexts = FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
        foreach (TMP_Text text in allTexts)
        {
            if (text != null && text.name == targetName)
                return text;
        }

        return null;
    }

    private static TMP_Text FindFundsLikeText()
    {
        TMP_Text[] allTexts = FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
        foreach (TMP_Text text in allTexts)
        {
            if (text == null)
                continue;

            string objectName = string.IsNullOrWhiteSpace(text.name) ? string.Empty : text.name.ToLowerInvariant();
            string label = string.IsNullOrWhiteSpace(text.text) ? string.Empty : text.text.ToLowerInvariant();

            if (objectName.Contains("fund") || label.Contains("fund"))
                return text;
        }

        return null;
    }

    private void BindGameManager()
    {
        GameManager resolved = FindObjectsByType<GameManager>(FindObjectsSortMode.None).FirstOrDefault();
        if (resolved == null)
            resolved = GameManager.Instance;

        if (resolved == gameManager)
            return;

        if (gameManager != null)
        {
            gameManager.FundsChanged -= HandleFundsChanged;
            gameManager.SeedsChanged -= HandleSeedsChanged;
        }

        gameManager = resolved;

        if (gameManager != null)
        {
            gameManager.FundsChanged += HandleFundsChanged;
            gameManager.SeedsChanged += HandleSeedsChanged;
        }
    }

    private void RefreshForCurrentScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (IsIntroScene(activeScene))
        {
            HideHudForIntroScene();
            return;
        }

        EnsureHudVisible();
        BindGameManager();
        AutoBindTextTargets();
        ApplyUnifiedHudStyle();
        RefreshAll();
    }

    private static bool IsIntroScene(Scene scene)
    {
        return scene.IsValid() && string.Equals(scene.name, IntroSceneName, System.StringComparison.Ordinal);
    }

    private void HideHudForIntroScene()
    {
        CleanupLegacyAutoFundsTexts();
        SetHudLabelActive(fundsText, false);
        SetHudLabelActive(seedsText, false);
    }

    private void EnsureHudVisible()
    {
        SetHudLabelActive(fundsText, true);
        SetHudLabelActive(seedsText, true);
    }

    private static void SetHudLabelActive(TMP_Text label, bool active)
    {
        if (label == null || label.gameObject == null)
            return;

        if (label.gameObject.activeSelf != active)
            label.gameObject.SetActive(active);
    }

    private void ApplyUnifiedHudStyle()
    {
        if (!enforceUnifiedHudStyle)
            return;

        TextMeshProUGUI fundsLabel = fundsText as TextMeshProUGUI;
        if (fundsLabel != null)
            ApplyUnifiedTextStyle(fundsLabel, fallbackFundsAnchoredPosition);

        TextMeshProUGUI seedsLabel = seedsText as TextMeshProUGUI;
        if (seedsLabel != null)
        {
            ApplyUnifiedTextStyle(seedsLabel, fallbackFundsAnchoredPosition);
            AlignSeedsTextBelowFunds();
        }
    }

    private void ApplyUnifiedTextStyle(TextMeshProUGUI label, Vector2 anchoredPosition)
    {
        if (label == null)
            return;

        label.fontSize = unifiedHudFontSize;
        label.color = unifiedHudTextColor;
        label.alignment = unifiedHudAlignment;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;

        RectTransform rect = label.rectTransform;
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = fallbackLabelSize;
    }
}
