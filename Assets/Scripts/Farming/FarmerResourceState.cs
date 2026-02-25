using UnityEngine;
using UnityEngine.SceneManagement;

public class FarmerResourceState : MonoBehaviour
{
    private static FarmerResourceState instance;

    private float maxEnergy = 100f;
    private float maxWater = 100f;
    private float energyRegenPerSecond = 8f;
    private string energyBarObjectName = "EnergyBar";
    private string waterBarObjectName = "WaterBar";

    private bool initialized;
    private bool farmerPresent;
    private float currentEnergy;
    private float currentWater;

    private ProgressBar energyBar;
    private ProgressBar waterBar;

    public static FarmerResourceState Instance
    {
        get
        {
            EnsureInstance();
            return instance;
        }
    }

    public bool IsInitialized => initialized;
    public float CurrentEnergy => currentEnergy;
    public float CurrentWater => currentWater;
    public float MaxEnergy => maxEnergy;
    public float MaxWater => maxWater;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    private static void EnsureInstance()
    {
        if (instance != null)
            return;

        GameObject go = new GameObject(nameof(FarmerResourceState));
        instance = go.AddComponent<FarmerResourceState>();
        DontDestroyOnLoad(go);
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
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
            SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Update()
    {
        if (!initialized)
            return;

        if (!farmerPresent && currentEnergy < maxEnergy)
        {
            currentEnergy = Mathf.Min(maxEnergy, currentEnergy + (energyRegenPerSecond * Time.deltaTime));
            ApplyValuesToBars();
        }
    }

    public void Configure(float maxEnergyValue, float maxWaterValue, float regenPerSecond, string energyBarName, string waterBarName)
    {
        maxEnergy = Mathf.Max(1f, maxEnergyValue);
        maxWater = Mathf.Max(1f, maxWaterValue);
        energyRegenPerSecond = Mathf.Max(0f, regenPerSecond);

        if (!string.IsNullOrWhiteSpace(energyBarName))
            energyBarObjectName = energyBarName;

        if (!string.IsNullOrWhiteSpace(waterBarName))
            waterBarObjectName = waterBarName;

        if (initialized)
        {
            currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);
            currentWater = Mathf.Clamp(currentWater, 0f, maxWater);
            ApplyValuesToBars();
        }
    }

    public void InitializeIfNeeded(float startingEnergy, float startingWater)
    {
        if (initialized)
            return;

        initialized = true;
        currentEnergy = Mathf.Clamp(startingEnergy, 0f, maxEnergy);
        currentWater = Mathf.Clamp(startingWater, 0f, maxWater);
        ApplyValuesToBars();
    }

    public void SetFarmerPresent(bool present)
    {
        farmerPresent = present;
    }

    public void SetEnergy(float value)
    {
        initialized = true;
        currentEnergy = Mathf.Clamp(value, 0f, maxEnergy);
        ApplyValuesToBars();
    }

    public void SetWater(float value)
    {
        initialized = true;
        currentWater = Mathf.Clamp(value, 0f, maxWater);
        ApplyValuesToBars();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        energyBar = null;
        waterBar = null;
        ApplyValuesToBars();
    }

    private void ApplyValuesToBars()
    {
        if (!initialized)
            return;

        ResolveBars();

        if (energyBar != null)
            energyBar.Fill = maxEnergy <= 0f ? 0f : currentEnergy / maxEnergy;

        if (waterBar != null)
            waterBar.Fill = maxWater <= 0f ? 0f : currentWater / maxWater;
    }

    private void ResolveBars()
    {
        if (energyBar != null && waterBar != null)
            return;

        ProgressBar[] bars = FindObjectsByType<ProgressBar>(FindObjectsSortMode.None);
        if (bars == null || bars.Length == 0)
            return;

        if (energyBar == null)
            energyBar = FindProgressBarByName(energyBarObjectName, bars);

        if (waterBar == null)
            waterBar = FindProgressBarByName(waterBarObjectName, bars);

        if (waterBar == null)
            waterBar = FindProgressBarByPartialName("water", bars);

        if (energyBar == null)
            energyBar = FindProgressBarByPartialName("energy", bars);

        if (waterBar == null)
            waterBar = bars[0];

        if (energyBar == null)
        {
            foreach (ProgressBar bar in bars)
            {
                if (bar != null && bar != waterBar)
                {
                    energyBar = bar;
                    break;
                }
            }
        }
    }

    private static ProgressBar FindProgressBarByName(string objectName, ProgressBar[] bars)
    {
        if (string.IsNullOrWhiteSpace(objectName) || bars == null || bars.Length == 0)
            return null;

        foreach (ProgressBar bar in bars)
        {
            if (bar != null && string.Equals(bar.name, objectName, System.StringComparison.Ordinal))
                return bar;
        }

        return null;
    }

    private static ProgressBar FindProgressBarByPartialName(string token, ProgressBar[] bars)
    {
        if (string.IsNullOrWhiteSpace(token) || bars == null || bars.Length == 0)
            return null;

        string loweredToken = token.ToLowerInvariant();
        foreach (ProgressBar bar in bars)
        {
            if (bar == null || string.IsNullOrWhiteSpace(bar.name))
                continue;

            if (bar.name.ToLowerInvariant().Contains(loweredToken))
                return bar;
        }

        return null;
    }
}
