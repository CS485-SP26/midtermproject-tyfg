using Character;
using Core;
using Farming;
using UnityEngine;

public partial class Farmer
{
    private void OnValidate()
    {
        MigrateLegacySerializedDataIfNeeded();

        maxEnergy = Mathf.Max(1f, maxEnergy);
        maxWater = Mathf.Max(1f, maxWater);
        startingEnergy = Mathf.Clamp(startingEnergy, 0f, maxEnergy);
        startingWater = Mathf.Clamp(startingWater, 0f, maxWater);

        energyRegenPerSecond = Mathf.Max(0f, energyRegenPerSecond);
        tillEnergyCost = Mathf.Max(0f, tillEnergyCost);
        jumpEnergyCost = Mathf.Max(0f, jumpEnergyCost);
        sprintEnergyDrainPerSecond = Mathf.Max(0f, sprintEnergyDrainPerSecond);
        waterPerUse = Mathf.Max(0f, waterPerUse);

        if (actionFeedbackSettings != null)
            actionFeedbackSettings.Clamp();

        seedsPerPlant = Mathf.Max(1, seedsPerPlant);
    }

    private void MigrateLegacySerializedDataIfNeeded()
    {
        if (!legacyToolVisualsMigrated)
        {
            toolVisuals.MigrateLegacyIfNeeded(wateringCan, gardenHoe);
            legacyToolVisualsMigrated = true;
        }

        if (!legacyFeedbackMigrated && actionFeedbackSettings != null)
        {
            actionFeedbackSettings.MigrateLegacy(
                notificationCanvas,
                feedbackAnchor,
                feedbackSize,
                feedbackFontSize,
                feedbackDurationSeconds,
                feedbackRisePixels,
                feedbackCooldownSeconds);
            legacyFeedbackMigrated = true;
        }
    }

    private void Start()
    {
        MigrateLegacySerializedDataIfNeeded();

        animatedController = GetComponent<AnimatedController>();
        movementController = GetComponent<MovementController>();
        Debug.Assert(animatedController, "Farmer requires an AnimatedController");
        Debug.Assert(movementController, "Farmer requires a MovementController");

        ApplyStatsDefinitionIfAssigned();
        ApplyLegacyWaterMigration();
        economyService = GameManager.Instance;
        actionFeedback = new FarmerActionFeedback(actionFeedbackSettings);
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
        runtimeResources.Configure(maxEnergy, maxWater, energyRegenPerSecond);
        SetEnergyLevel(initialEnergy);
        SetWaterLevel(initialWater);
        SetTool("None");
    }

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

    private void OnDisable()
    {
        if (energyState != null)
            energyState.SetActorPresent(false);

        if (resourceState != null)
            resourceState.SetFarmerPresent(false);
    }

    private void OnDestroy()
    {
        if (energyState != null)
            energyState.SetActorPresent(false);

        if (resourceState != null)
            resourceState.SetFarmerPresent(false);
    }

    private void Update()
    {
        DrainSprintEnergyIfNeeded();
        RegenerateEnergyIfIdle();
    }

    private void ApplyLegacyWaterMigration()
    {
        if (!migrateLegacyWaterValues || maxWater <= 1f)
            return;

        if (startingWater > 0f && startingWater <= 1f)
            startingWater *= maxWater;

        if (waterPerUse > 0f && waterPerUse <= 1f)
            waterPerUse *= maxWater;

        startingWater = Mathf.Clamp(startingWater, 0f, maxWater);
    }

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
}
