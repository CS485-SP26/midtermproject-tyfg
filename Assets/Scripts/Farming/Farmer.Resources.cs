using Core;
using UnityEngine;

public partial class Farmer
{

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

    private void RegenerateEnergyIfIdle()
    {
        bool isActivelySprinting = sprintInputHeld && movementController != null && movementController.HasMovementInput;
        if (!runtimeResources.TryRegenerateEnergy(isActivelySprinting, Time.deltaTime))
            return;

        PushEnergyToStateSystems();
    }

    private bool TryConsumeEnergy(float amount)
    {
        if (!runtimeResources.TryConsumeEnergy(amount))
            return false;

        PushEnergyToStateSystems();
        return true;
    }

    private bool TryConsumeWater(float amount)
    {
        if (!runtimeResources.TryConsumeWater(amount))
            return false;

        PushWaterToStateSystems();
        return true;
    }

    private bool TryConsumeSeeds(int amount)
    {
        if (amount <= 0)
            return true;

        if (economyService == null)
            economyService = GameManager.Instance;

        return economyService != null && economyService.TrySpendResource(EconomyResource.Seeds, amount);
    }

    private void SetEnergyLevel(float value)
    {
        runtimeResources.SetEnergy(value);
        PushEnergyToStateSystems();
    }

    private void SetWaterLevel(float value)
    {
        runtimeResources.SetWater(value);
        PushWaterToStateSystems();
    }

    private void PushEnergyToStateSystems()
    {
        float value = runtimeResources.CurrentEnergy;
        if (energyState != null)
            energyState.SetValue(value);

        if (resourceState != null)
            resourceState.SetEnergy(value);
    }

    private void PushWaterToStateSystems()
    {
        if (resourceState != null)
            resourceState.SetWater(runtimeResources.CurrentWater);
    }
}
