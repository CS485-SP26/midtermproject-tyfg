using Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class CurrencyTextUI : MonoBehaviour
{
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

    private GameManager gameManager;
    private int lastRenderedFunds = int.MinValue;
    private int lastRenderedSeeds = int.MinValue;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
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
            Destroy(gameObject);
            return;
        }

        instance = this;
       // DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        BindGameManager();
        AutoBindTextTargets();
        RefreshAll();
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
        BindGameManager();
        AutoBindTextTargets();
        RefreshAll();
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name == "Scene0-Intro")
            return;

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
        if (fundsText == null)
            fundsText = FindTextByName(fundsObjectName);

        if (seedsText == null)
            seedsText = FindTextByName(seedsObjectName);

        if (seedsText == null)
            seedsText = CreateSeedsTextBelowFunds();
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
        seedsRect.anchoredPosition = fundsRect.anchoredPosition + new Vector2(0f, -(lineHeight + seedLabelVerticalSpacing));

        return seedsLabelText;
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

    private void BindGameManager()
    {
        GameManager resolved = FindObjectsByType<GameManager>(FindObjectsSortMode.None).FirstOrDefault();
        if (resolved == null && SceneManager.GetActiveScene().name != "Scene0-Intro")
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
}
