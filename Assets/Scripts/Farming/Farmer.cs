using UnityEngine;
using Farming;
using Character;
using UnityEngine.SceneManagement;
using Core;
using Unity.Multiplayer.Center.Common.Analytics;

/*
* The Farmer class manages the player's farming-related actions, resources, and UI.
* It handles energy and water resources, tool visuals, and interactions with FarmTiles.
* It also provides feedback when actions are blocked due to insufficient resources.
* Exposes:
*   - SetTool(string tool): Sets the active tool visual based on the provided tool name.
*   - SetSprintInput(bool sprintPressed): Updates the sprint input state and manages sprinting based on energy levels.
*   - TryConsumeJumpEnergy(): Attempts to consume energy for jumping and returns true if successful, false if not enough energy.
*   - TryTileInteraction(FarmTile tile): Attempts to interact with the given FarmTile based on its condition, consuming resources as needed and providing feedback if resources are insufficient.
*   - RefillWaterToFull(): Refills the water resource to its maximum level.
* Requires:
*   - An AnimatedController component for triggering animations.
*   - A MovementController component for managing movement and sprinting.
*   - A FarmerResourceState singleton for managing resource state across the game.
*   - ProgressBar UI components for displaying energy and water levels (optional, will auto-bind if not assigned).
*   - GameObjects for tool visuals (watering can and garden hoe) to show/hide based on the active tool.
*   - A Canvas for displaying action feedback messages (optional, will search for an active canvas if not assigned).
*/

public class Farmer : MonoBehaviour, IFarmActor
{
    [Header("Shared Actor Stats")]
    [SerializeField] private FarmActorStatsDefinition statsDefinition;

    [Header("Tool Visuals")]
    [SerializeField] private GameObject wateringCan;
    [SerializeField] private GameObject gardenHoe;

    [Header("Resource UI")]
    [SerializeField] private ProgressBar energyLevelUI;
    [SerializeField] private ProgressBar waterLevelUI;
    [SerializeField] private string energyBarObjectName = "EnergyBar";
    [SerializeField] private string waterBarObjectName = "WaterBar";
    [SerializeField] private Color staminaBarColor = new Color(1f, 0.86f, 0.2f, 1f);
    [SerializeField] private Color waterBarColor = new Color(0.2f, 0.38f, 0.88f, 1f);

    [Header("Energy")]
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField] private float startingEnergy = 100f;
    [SerializeField] private float energyRegenPerSecond = 8f;
    [SerializeField] private float tillEnergyCost = 15f;
    [SerializeField] private float jumpEnergyCost = 12f;
    [SerializeField] private float sprintEnergyDrainPerSecond = 15f;

    [Header("Water")]
    [SerializeField] private float maxWater = 100f;
    [SerializeField] private float startingWater = 100f;
    [SerializeField] private float waterPerUse = 10f;
    [SerializeField] private bool migrateLegacyWaterValues = true;

    [Header("Action Feedback")]
    [SerializeField] private Canvas notificationCanvas;
    [SerializeField] private Vector2 feedbackAnchor = new Vector2(0.5f, 0.28f);
    [SerializeField] private Vector2 feedbackSize = new Vector2(420f, 48f);
    [SerializeField] private int feedbackFontSize = 20;
    [SerializeField] private float feedbackDurationSeconds = 0.8f;
    [SerializeField] private float feedbackRisePixels = 28f;
    [SerializeField] private float feedbackCooldownSeconds = 0.6f;
    [SerializeField] private string lowEnergyMessage = "Not enough energy.";
    [SerializeField] private string lowWaterMessage = "Out of water. Refill at the shed.";
    [SerializeField] private string lowSeedsMessage = "Out of seeds. Buy more seeds.";
    [SerializeField] private int seedsPerPlant = 1;

    private AnimatedController animatedController;
    private MovementController movementController;
    private FarmerResourceState resourceState;
    private EnergyState energyState;
    private float currentEnergy;
    private float currentWater;
    private bool sprintInputHeld;
    private float nextFeedbackTime;
    private bool warnedMissingEnergyBar;
    private bool warnedMissingWaterBar;
    private IEconomyService economyService;
    private readonly FarmerTileInteractionService tileInteractionService = new FarmerTileInteractionService();

    // Clamps serialized values to safe runtime ranges when edited in inspector.
    private void OnValidate()
    {
        maxEnergy = Mathf.Max(1f, maxEnergy);
        maxWater = Mathf.Max(1f, maxWater);
        startingEnergy = Mathf.Clamp(startingEnergy, 0f, maxEnergy);
        startingWater = Mathf.Clamp(startingWater, 0f, maxWater);

        energyRegenPerSecond = Mathf.Max(0f, energyRegenPerSecond);
        tillEnergyCost = Mathf.Max(0f, tillEnergyCost);
        jumpEnergyCost = Mathf.Max(0f, jumpEnergyCost);
        sprintEnergyDrainPerSecond = Mathf.Max(0f, sprintEnergyDrainPerSecond);
        waterPerUse = Mathf.Max(0f, waterPerUse);

        feedbackFontSize = Mathf.Max(10, feedbackFontSize);
        feedbackDurationSeconds = Mathf.Max(0.1f, feedbackDurationSeconds);
        feedbackRisePixels = Mathf.Max(0f, feedbackRisePixels);
        feedbackCooldownSeconds = Mathf.Max(0.05f, feedbackCooldownSeconds);
        seedsPerPlant = Mathf.Max(1, seedsPerPlant);
    }

    // Caches dependencies, restores persisted resources, and initializes tool/UI state.
    private void Start()
    {
        animatedController = GetComponent<AnimatedController>();
        movementController = GetComponent<MovementController>();
        Debug.Assert(animatedController, "Farmer requires an AnimatedController");
        Debug.Assert(movementController, "Farmer requires a MovementController");

        ApplyStatsDefinitionIfAssigned();
        ApplyLegacyWaterMigration();
        economyService = GameManager.Instance;
        energyState = EnergyState.Instance;
        if (energyState != null)
        {
            energyState.Configure(maxEnergy, energyRegenPerSecond);
            energyState.InitializeIfNeeded(startingEnergy);
            energyState.SetActorPresent(true);
        }

        resourceState = FarmerResourceState.Instance;
        if (resourceState != null)
        {
            resourceState.SetFarmerPresent(true);
            resourceState.Configure(maxEnergy, maxWater, energyRegenPerSecond, energyBarObjectName, waterBarObjectName);
            resourceState.InitializeIfNeeded(startingEnergy, startingWater);
        }

        float initialEnergy = energyState != null && energyState.IsInitialized ? energyState.Current : startingEnergy;
        float initialWater = resourceState != null && resourceState.IsInitialized ? resourceState.CurrentWater : startingWater;
        SetEnergyLevel(initialEnergy);
        SetWaterLevel(initialWater);
        SetTool("None");
    }

    // Marks that a farmer is currently active so external regen logic can pause.
    private void OnEnable()
    {
        if (resourceState == null)
            resourceState = FarmerResourceState.Instance;

        if (energyState == null)
            energyState = EnergyState.Instance;

        if (energyState != null)
            energyState.SetActorPresent(true);

        if (resourceState != null)
            resourceState.SetFarmerPresent(true);
    }

    // Marks farmer as inactive when component is disabled.
    private void OnDisable()
    {
        if (energyState != null)
            energyState.SetActorPresent(false);

        if (resourceState != null)
            resourceState.SetFarmerPresent(false);
    }

    // Ensures farmer-presence state is reset if object is destroyed.
    private void OnDestroy()
    {
        if (energyState != null)
            energyState.SetActorPresent(false);

        if (resourceState != null)
            resourceState.SetFarmerPresent(false);
    }

    // Per-frame stamina drain/regeneration loop.
    private void Update()
    {
        DrainSprintEnergyIfNeeded();
        RegenerateEnergyIfIdle();
    }

    // Activates the matching tool model and hides all others.
    public void SetTool(string tool)
    {
        Debug.Log("SetTool called with: " + tool);

        if (wateringCan != null)
            wateringCan.SetActive(false);

        if (gardenHoe != null)
            gardenHoe.SetActive(false);

        switch (tool)
        {
            case "GardenHoe":
                if (gardenHoe != null)
                    gardenHoe.SetActive(true);
                break;

            case "WaterCan":
                if (wateringCan != null)
                    wateringCan.SetActive(true);
                break;
        }
    }

    // Accepts sprint input and blocks sprint when out of energy.
    public void SetSprintInput(bool sprintPressed)
    {
        sprintInputHeld = sprintPressed;

        if (movementController == null)
            return;

        if (!sprintPressed)
        {
            movementController.SetSprint(false);
            return;
        }

        if (currentEnergy <= 0f)
        {
            sprintInputHeld = false;
            movementController.SetSprint(false);
            ShowActionBlockedFeedback(lowEnergyMessage);
        }
    }

    // Attempts to spend jump energy and reports failure feedback.
    public bool TryConsumeJumpEnergy()
    {
        if (TryConsumeEnergy(jumpEnergyCost))
            return true;

        ShowActionBlockedFeedback(lowEnergyMessage);
        return false;
    }

    // Handles interaction behavior for the selected tile condition.
    public void TryTileInteraction(FarmTile tile)
    {
        if (tile == null)
            return;

        if (tile.TryGetComponent<SeedPurchaseTile>(out SeedPurchaseTile purchaseTile))
        {
            purchaseTile.TryPurchaseFromFarmer(this);
            return;
        }

        tileInteractionService.TryInteract(this, tile);
    }

    // Refills player water resource to full capacity.
    public void RefillWaterToFull()
    {
        SetWaterLevel(maxWater);
    }

    // Applies sprint energy drain when sprinting with movement input.
    private void DrainSprintEnergyIfNeeded()
    {
        bool hasMovementInput = movementController != null && movementController.HasMovementInput;
        bool shouldSprint = sprintInputHeld && hasMovementInput;

        if (shouldSprint && !TryConsumeEnergy(sprintEnergyDrainPerSecond * Time.deltaTime))
        {
            shouldSprint = false;
            sprintInputHeld = false;
            ShowActionBlockedFeedback(lowEnergyMessage);
        }

        if (movementController != null)
            movementController.SetSprint(shouldSprint);
    }

    // Restores energy over time while not actively sprinting.
    private void RegenerateEnergyIfIdle()
    {
        bool isActivelySprinting = sprintInputHeld && movementController != null && movementController.HasMovementInput;
        if (isActivelySprinting || energyRegenPerSecond <= 0f || currentEnergy >= maxEnergy)
            return;

        SetEnergyLevel(currentEnergy + (energyRegenPerSecond * Time.deltaTime));
    }

    // Tries to spend energy; returns false if balance is insufficient.
    private bool TryConsumeEnergy(float amount)
    {
        if (amount <= 0f)
            return true;

        if (currentEnergy + 0.001f < amount)
            return false;

        SetEnergyLevel(currentEnergy - amount);
        return true;
    }

    // Tries to spend water; returns false if balance is insufficient.
    private bool TryConsumeWater(float amount)
    {
        if (amount <= 0f)
            return true;

        if (currentWater + 0.001f < amount)
            return false;

        SetWaterLevel(currentWater - amount);
        return true;
    }

    // Tries to spend seeds from the economy; returns false if balance is insufficient.
    private bool TryConsumeSeeds(int amount)
    {
        if (amount <= 0)
            return true;

        if (economyService == null)
            economyService = GameManager.Instance;

        return economyService != null && economyService.TrySpendResource(EconomyResource.Seeds, amount);
    }

    // Updates current energy and shared resource-state mirrors.
    private void SetEnergyLevel(float value)
    {
        currentEnergy = Mathf.Clamp(value, 0f, maxEnergy);

        if (energyState != null)
            energyState.SetValue(currentEnergy);

        if (resourceState != null)
            resourceState.SetEnergy(currentEnergy);
    }

    // Updates current water and shared resource-state mirror.
    private void SetWaterLevel(float value)
    {
        currentWater = Mathf.Clamp(value, 0f, maxWater);

        if (resourceState != null)
            resourceState.SetWater(currentWater);
    }

    // Spawns a temporary floating message for blocked actions.
    private void ShowActionBlockedFeedback(string message)
    {
        if (string.IsNullOrWhiteSpace(message) || Time.time < nextFeedbackTime)
            return;

        nextFeedbackTime = Time.time + feedbackCooldownSeconds;
        IActionFeedbackService feedbackService = ActionFeedbackService.Instance;
        if (feedbackService == null)
            return;

        feedbackService.ShowFeedback(
            message,
            false,
            notificationCanvas,
            feedbackAnchor,
            feedbackSize,
            feedbackFontSize,
            feedbackDurationSeconds,
            feedbackRisePixels,
            Color.white,
            "FarmerFeedback");
    }

    // Auto-locates energy/water progress bars in scene by explicit names/fallbacks.
    private void AutoBindProgressBars()
    {
        ProgressBar[] bars = FindProgressBarsInCurrentScene();
        if (bars.Length == 0)
            return;

        if (energyLevelUI == null)
            energyLevelUI = FindProgressBarByName(energyBarObjectName, bars) ??
                            FindProgressBarByPartialName("energy", bars) ??
                            FindProgressBarByPartialName("stamina", bars);

        if (waterLevelUI == null)
            waterLevelUI = FindProgressBarByName(waterBarObjectName, bars) ??
                           FindProgressBarByPartialName("water", bars);

        if (energyLevelUI == null && waterLevelUI == null)
            energyLevelUI = FindFirstNonNullBar(bars);

        if (energyLevelUI != null && waterLevelUI == null)
            waterLevelUI = FindFirstDistinctBar(energyLevelUI, bars);

        if (waterLevelUI != null && energyLevelUI == null)
            energyLevelUI = FindFirstDistinctBar(waterLevelUI, bars);

        if (energyLevelUI == waterLevelUI)
            waterLevelUI = null;
    }

    // Finds progress bars scoped to current scene roots (with global fallback).
    private ProgressBar[] FindProgressBarsInCurrentScene()
    {
        Scene scene = gameObject.scene;
        if (!scene.IsValid())
            return FindObjectsByType<ProgressBar>(FindObjectsSortMode.None);

        GameObject[] roots = scene.GetRootGameObjects();
        int count = 0;
        foreach (GameObject root in roots)
        {
            if (root == null)
                continue;

            count += root.GetComponentsInChildren<ProgressBar>(true).Length;
        }

        if (count == 0)
            return FindObjectsByType<ProgressBar>(FindObjectsSortMode.None);

        ProgressBar[] result = new ProgressBar[count];
        int index = 0;
        foreach (GameObject root in roots)
        {
            if (root == null)
                continue;

            ProgressBar[] bars = root.GetComponentsInChildren<ProgressBar>(true);
            foreach (ProgressBar bar in bars)
            {
                result[index++] = bar;
            }
        }

        return result;
    }

    // Finds a progress bar with exact object-name match.
    private static ProgressBar FindProgressBarByName(string objectName, ProgressBar[] bars)
    {
        if (string.IsNullOrWhiteSpace(objectName) || bars == null || bars.Length == 0)
            return null;

        foreach (ProgressBar bar in bars)
        {
            if (bar != null && bar.name == objectName)
                return bar;
        }

        return null;
    }

    // Finds first progress bar whose object name contains a token.
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

    // Creates missing companion bar when only one of energy/water bars exists.
    private void EnsureBothProgressBars()
    {
        if (energyLevelUI != null && waterLevelUI != null && energyLevelUI != waterLevelUI)
            return;

        ProgressBar[] bars = FindProgressBarsInCurrentScene();

        if (energyLevelUI == null && waterLevelUI == null)
            energyLevelUI = FindFirstNonNullBar(bars);

        if (energyLevelUI == null && waterLevelUI != null)
        {
            energyLevelUI = FindFirstDistinctBar(waterLevelUI, bars);
            if (energyLevelUI == null)
                energyLevelUI = CloneCompanionBar(waterLevelUI, energyBarObjectName, new Vector2(0f, 36f));

            if (energyLevelUI != null)
                ApplyEnergyBarStyle(energyLevelUI);
        }

        if (waterLevelUI == null && energyLevelUI != null)
        {
            waterLevelUI = FindFirstDistinctBar(energyLevelUI, bars);
            if (waterLevelUI == null)
                waterLevelUI = CloneCompanionBar(energyLevelUI, waterBarObjectName, new Vector2(0f, -36f));

            if (waterLevelUI != null)
                ApplyWaterBarStyle(waterLevelUI);
        }

        if (energyLevelUI != null && waterLevelUI != null && energyLevelUI == waterLevelUI)
        {
            waterLevelUI = FindFirstDistinctBar(energyLevelUI, bars);
            if (waterLevelUI == null)
                waterLevelUI = CloneCompanionBar(energyLevelUI, waterBarObjectName, new Vector2(0f, -36f));
        }

        if (energyLevelUI != null)
            ApplyEnergyBarStyle(energyLevelUI);

        if (waterLevelUI != null)
            ApplyWaterBarStyle(waterLevelUI);
    }

    // Re-resolves bars after scene/UI transitions and ensures references do not collapse to the same bar.
    private void EnsureProgressBarsBound()
    {
        bool energyValid = energyLevelUI != null;
        bool waterValid = waterLevelUI != null;

        if (energyValid && waterValid && energyLevelUI != waterLevelUI)
            return;

        AutoBindProgressBars();
        EnsureBothProgressBars();

        if (energyLevelUI != null)
            ApplyEnergyBarStyle(energyLevelUI);

        if (waterLevelUI != null)
            ApplyWaterBarStyle(waterLevelUI);

        if (energyLevelUI == null && !warnedMissingEnergyBar)
        {
            Debug.LogWarning("Farmer could not bind EnergyBar by name. Check HUD object naming/persistence.");
            warnedMissingEnergyBar = true;
        }

        if (waterLevelUI == null && !warnedMissingWaterBar)
        {
            Debug.LogWarning("Farmer could not bind WaterBar by name. Check HUD object naming/persistence.");
            warnedMissingWaterBar = true;
        }
    }

    private void ResolveNamedWaterBar()
    {
        if (waterLevelUI != null)
            return;

        ProgressBar[] bars = FindProgressBarsInCurrentScene();
        waterLevelUI = FindProgressBarByName(waterBarObjectName, bars) ??
                       FindProgressBarByPartialName("water", bars);

        if (waterLevelUI == null && energyLevelUI != null)
            waterLevelUI = FindFirstDistinctBar(energyLevelUI, bars);

        if (waterLevelUI == null && energyLevelUI != null)
            waterLevelUI = CloneCompanionBar(energyLevelUI, waterBarObjectName, new Vector2(0f, -36f));

        if (waterLevelUI != null)
            ApplyWaterBarStyle(waterLevelUI);
    }

    private void ApplyEnergyBarStyle(ProgressBar bar)
    {
        if (bar == null)
            return;

        if (!string.IsNullOrWhiteSpace(energyBarObjectName))
            bar.name = energyBarObjectName;

        bar.SetText("Energy");
        bar.SetFillColor(staminaBarColor);
    }

    private void ApplyWaterBarStyle(ProgressBar bar)
    {
        if (bar == null)
            return;

        if (!string.IsNullOrWhiteSpace(waterBarObjectName))
            bar.name = waterBarObjectName;

        bar.SetText("Water Level");
        bar.SetFillColor(waterBarColor);
    }

    // Clones a template progress bar and offsets it for paired HUD layout.
    private static ProgressBar CloneCompanionBar(ProgressBar template, string objectName, Vector2 positionOffset)
    {
        if (template == null)
            return null;

        Transform parent = template.transform.parent;
        GameObject clone = UnityEngine.Object.Instantiate(template.gameObject, parent);
        clone.name = string.IsNullOrWhiteSpace(objectName) ? $"{template.name}_Clone" : objectName;

        RectTransform templateRect = template.GetComponent<RectTransform>();
        RectTransform cloneRect = clone.GetComponent<RectTransform>();
        if (templateRect != null && cloneRect != null)
            cloneRect.anchoredPosition = templateRect.anchoredPosition + positionOffset;

        return clone.GetComponent<ProgressBar>();
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

    // Returns first non-null bar that is not the provided reference.
    private static ProgressBar FindFirstDistinctBar(ProgressBar referenceBar, ProgressBar[] bars)
    {
        if (bars == null || bars.Length == 0)
            return null;

        foreach (ProgressBar bar in bars)
        {
            if (bar != null && bar != referenceBar)
                return bar;
        }

        return null;
    }

    // Migrates old normalized-water serialized values to absolute-water units.
    private void ApplyLegacyWaterMigration()
    {
        if (!migrateLegacyWaterValues || maxWater <= 1f)
            return;

        // Previous versions serialized water as normalized [0..1].
        if (startingWater > 0f && startingWater <= 1f)
            startingWater *= maxWater;

        if (waterPerUse > 0f && waterPerUse <= 1f)
            waterPerUse *= maxWater;

        startingWater = Mathf.Clamp(startingWater, 0f, maxWater);
    }

    // Applies shared actor stats from ScriptableObject if assigned.
    private void ApplyStatsDefinitionIfAssigned()
    {
        if (statsDefinition == null)
            return;

        maxEnergy = statsDefinition.MaxEnergy;
        startingEnergy = statsDefinition.StartingEnergy;
        energyRegenPerSecond = statsDefinition.EnergyRegenPerSecond;
        tillEnergyCost = statsDefinition.TillEnergyCost;
        jumpEnergyCost = statsDefinition.JumpEnergyCost;
        sprintEnergyDrainPerSecond = statsDefinition.SprintEnergyDrainPerSecond;

        maxWater = statsDefinition.MaxWater;
        startingWater = statsDefinition.StartingWater;
        waterPerUse = statsDefinition.WaterPerUse;

        seedsPerPlant = statsDefinition.SeedsPerPlant;
    }

    public float TillEnergyCost => tillEnergyCost;
    public float WaterPerUse => waterPerUse;
    public int SeedsPerPlant => seedsPerPlant;
    public string LowEnergyMessage => lowEnergyMessage;
    public string LowWaterMessage => lowWaterMessage;
    public string LowSeedsMessage => lowSeedsMessage;

    public bool TryConsumeEnergyForAction(float amount)
    {
        return TryConsumeEnergy(amount);
    }

    public bool TryConsumeWaterForAction(float amount)
    {
        return TryConsumeWater(amount);
    }

    public bool TryConsumeSeedsForAction(int amount)
    {
        return TryConsumeSeeds(amount);
    }

    public void TriggerAnimation(string triggerName)
    {
        if (animatedController == null || string.IsNullOrWhiteSpace(triggerName))
            return;

        animatedController.SetTrigger(triggerName);
    }

    public void ShowActionBlockedMessage(string message)
    {
        ShowActionBlockedFeedback(message);
    }
}
