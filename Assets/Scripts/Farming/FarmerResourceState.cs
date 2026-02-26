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

    // Finds a progress bar by partial-name token.
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
