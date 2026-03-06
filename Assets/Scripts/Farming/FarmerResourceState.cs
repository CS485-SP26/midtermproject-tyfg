using UnityEngine;
using UnityEngine.SceneManagement;

/*
* This class manages the player's energy and water resources, including their current values, maximum values, and regeneration over time.
* It also handles updating the UI progress bars that display the current energy and water levels.
* The class is implemented as a singleton to allow easy access from other scripts, and it persists across scene loads to maintain resource state throughout the game.
* Exposes:
*   - CurrentEnergy (float): The current energy level of the player.
*   - CurrentWater (float): The current water level of the player.
*   - MaxEnergy (float): The maximum energy level of the player.
*   - MaxWater (float): The maximum water level of the player.
*   - SetEnergy(float value): Sets the current energy level to the specified value, clamped between 0 and MaxEnergy.
*   - SetWater(float value): Sets the current water level to the specified value, clamped between 0 and MaxWater.
*   - Configure(float maxEnergyValue, float maxWaterValue, float regenPerSecond, string energyBarName, string waterBarName): 
        Configures the maximum values, regeneration rate, and UI bar names for energy and water.
*   - InitializeIfNeeded(float startingEnergy, float startingWater): Initializes the current energy and water levels if they haven't been 
        initialized yet, using the provided starting values.
*   - SetFarmerPresent(bool present): Sets whether the farmer is currently present, which affects whether energy regeneration occurs.
* Requires:
*   - ProgressBar components in the scene with names matching energyBarObjectName and waterBarObjectName, or with names containing 
        "energy" and "water" respectively, for the UI to display the resource levels.
*   - The class must be accessed through the Instance property to ensure the singleton pattern is maintained and the instance is 
        properly initialized. Directly adding this script to a GameObject in the scene is not recommended, as it will be automatically created and managed by the class itself.
*/

public class FarmerResourceState : MonoBehaviour
{
    private static FarmerResourceState instance;
    private static readonly Vector2 EnergyBarOffset = new Vector2(0f, 36f);
    private static readonly Vector2 WaterBarOffset = new Vector2(0f, -36f);
    private static readonly Color EnergyBarColor = new Color(1f, 0.86f, 0.2f, 1f);
    private static readonly Color WaterBarColor = new Color(0.2f, 0.38f, 0.88f, 1f);
    private const string EnergyLabel = "Energy";
    private const string WaterLabel = "Water Level";
    private const string DefaultTemplateBarName = "ProgressBar";

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

    // Clears static singleton references on play-mode/runtime subsystem reset.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    // Ensures singleton instance exists after scene load.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    // Creates singleton GameObject when no instance exists yet.
    private static void EnsureInstance()
    {
        if (instance != null)
            return;

        GameObject go = new GameObject(nameof(FarmerResourceState));
        instance = go.AddComponent<FarmerResourceState>();
        DontDestroyOnLoad(go);
    }

    // Enforces singleton and subscribes to scene-load rebinding.
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

    // Removes scene-load subscription on destroy.
    private void OnDestroy()
    {
        if (instance == this)
            SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    // Regenerates energy while no farmer is active in the scene.
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

    // Applies configuration values used for clamping/regen/bar lookup.
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

    // Initializes persisted values exactly once for a new play session.
    public void InitializeIfNeeded(float startingEnergy, float startingWater)
    {
        if (initialized)
            return;

        initialized = true;
        currentEnergy = Mathf.Clamp(startingEnergy, 0f, maxEnergy);
        currentWater = Mathf.Clamp(startingWater, 0f, maxWater);
        ApplyValuesToBars();
    }

    // Tracks whether an active Farmer is currently controlling resources directly.
    public void SetFarmerPresent(bool present)
    {
        farmerPresent = present;
    }

    // Writes current energy value and refreshes UI bars.
    public void SetEnergy(float value)
    {
        initialized = true;
        currentEnergy = Mathf.Clamp(value, 0f, maxEnergy);
        ApplyValuesToBars();
    }

    // Writes current water value and refreshes UI bars.
    public void SetWater(float value)
    {
        initialized = true;
        currentWater = Mathf.Clamp(value, 0f, maxWater);
        ApplyValuesToBars();
    }

    // Clears cached bars on scene load then reapplies latest values.
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        energyBar = null;
        waterBar = null;
        ApplyValuesToBars();
    }

    // Pushes normalized energy/water values to resolved progress bars.
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

    // Resolves missing bar references from current scene objects.
    private void ResolveBars()
    {
        if (BarsAreBoundAndDistinct())
            return;

        ProgressBar[] bars = FindObjectsByType<ProgressBar>(FindObjectsSortMode.None);
        if (bars == null || bars.Length == 0)
            return;

        energyBar = FindProgressBarByName(energyBarObjectName, bars);
        waterBar = FindProgressBarByName(waterBarObjectName, bars);

        if (energyBar == null && waterBar == null)
        {
            ProgressBar template = FindProgressBarByName(DefaultTemplateBarName, bars);
            if (template == null)
                template = FindFirstNonNullBar(bars);

            if (template != null)
            {
                energyBar = template;
                if (!string.IsNullOrWhiteSpace(energyBarObjectName))
                    energyBar.name = energyBarObjectName;
            }
        }

        if (energyBar != null && waterBar == null)
            waterBar = CloneCompanionBar(energyBar, waterBarObjectName, WaterBarOffset);

        if (waterBar != null && energyBar == null)
            energyBar = CloneCompanionBar(waterBar, energyBarObjectName, EnergyBarOffset);

        if (energyBar != null && waterBar != null && energyBar == waterBar)
            waterBar = CloneCompanionBar(energyBar, waterBarObjectName, WaterBarOffset);

        ApplyEnergyBarStyle(energyBar);
        ApplyWaterBarStyle(waterBar);
    }

    // Finds a progress bar by exact name.
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

    // Returns first non-null progress bar candidate.
    private static ProgressBar FindFirstNonNullBar(ProgressBar[] bars)
    {
        if (bars == null || bars.Length == 0)
            return null;

        foreach (ProgressBar bar in bars)
        {
            if (bar != null)
                return bar;
        }

        return null;
    }

    // Returns true when both bars are valid and bound to different objects.
    private bool BarsAreBoundAndDistinct()
    {
        return energyBar != null && waterBar != null && energyBar != waterBar;
    }

    // Clones a template progress bar and offsets it for paired HUD layout.
    private static ProgressBar CloneCompanionBar(ProgressBar template, string objectName, Vector2 positionOffset)
    {
        if (template == null)
            return null;

        Transform parent = template.transform.parent;
        GameObject clone = Instantiate(template.gameObject, parent);
        clone.name = string.IsNullOrWhiteSpace(objectName) ? $"{template.name}_Clone" : objectName;

        RectTransform templateRect = template.GetComponent<RectTransform>();
        RectTransform cloneRect = clone.GetComponent<RectTransform>();
        if (templateRect != null && cloneRect != null)
            cloneRect.anchoredPosition = templateRect.anchoredPosition + positionOffset;

        return clone.GetComponent<ProgressBar>();
    }

    // Applies canonical energy bar style.
    private static void ApplyEnergyBarStyle(ProgressBar bar)
    {
        if (bar == null)
            return;

        bar.SetText(EnergyLabel);
        bar.SetFillColor(EnergyBarColor);
    }

    // Applies canonical water bar style.
    private static void ApplyWaterBarStyle(ProgressBar bar)
    {
        if (bar == null)
            return;

        bar.SetText(WaterLabel);
        bar.SetFillColor(WaterBarColor);
    }
}
