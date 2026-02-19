using Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    private GameManager gameManager;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
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
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        gameManager = GameManager.Instance;
        gameManager.FundsChanged += HandleFundsChanged;
        gameManager.SeedsChanged += HandleSeedsChanged;
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
        AutoBindTextTargets();
        RefreshAll();
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
    }

    private void UpdateSeedsText(int seeds)
    {
        if (seedsText == null)
            AutoBindTextTargets();

        if (seedsText != null)
            seedsText.text = $"{seedsLabel} {seeds}";
    }

    private void AutoBindTextTargets()
    {
        if (fundsText == null)
            fundsText = FindTextByName(fundsObjectName);

        if (seedsText == null)
            seedsText = FindTextByName(seedsObjectName);
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
}
