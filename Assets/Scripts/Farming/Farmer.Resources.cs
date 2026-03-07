using Core;
using UnityEngine;

/*
* This partial class tells the compiler that the Farmer class
* is defined across multiple files. This is purely an 
* organizational tool to keep related code together without making one file too large. The Farmer class itself is the main
* controller for the player's farming character, handling input,
* movement, and interactions. By splitting it into multiple 
* files, we can keep the codebase more manageable and focused. 
* This particular file contains methods related to managing the farmer's resources.
*     - This includes SPRINT ENERGY, which is consumed when the 
*       player sprints and regenerates when idle.
*     - WATER, which is consumed when watering crops.
*     - SEEDS, which are consumed when planting crops.
* The file also handles the logic for ENERGY REGENERATION and
* Pushing resource updates to shared state systems.
*/
public partial class Farmer
{
    /*
    * This method determines whether the player should be 
    * sprinting and handles the energy cost.
    */
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

    /*
    *  This method regenerates energy only when the player is not
    *  actively sprinting. This encourages strategic use of 
    * sprinting
    */
    private void RegenerateEnergyIfIdle()
    {
        bool isActivelySprinting = sprintInputHeld && movementController != null && movementController.HasMovementInput;
        if (!runtimeResources.TryRegenerateEnergy(isActivelySprinting, Time.deltaTime))
            return;

        PushEnergyToStateSystems();
        // NOTE: We do not stop regeneraton when consuming energy,
        // by other methods (e.g. tilling)
    }

    /*
    * This is the canonical way to spend energy.
    */
    private bool TryConsumeEnergy(float amount)
    {
        if (!runtimeResources.TryConsumeEnergy(amount))
            return false;

        PushEnergyToStateSystems();
        return true;
    }

    /* 
    * This is the canonical way to spend water.
    */
    private bool TryConsumeWater(float amount)
    {
        if (!runtimeResources.TryConsumeWater(amount))
            return false;

        PushWaterToStateSystems();
        return true;
    }

    /*
    * Attempts to spend seeds using the global economy system.
    *
    * In the current implementation, seeds are treated as a shared economy resource
    * rather than something stored on the Farmer. This method acts as the Farmer's
    * gateway for planting actions that require seeds, delegating the actual resource
    * check and deduction to IEconomyService.
    *
    * If the design later shifts toward seeds being per-Farmer (e.g., inventory-based),
    * this method would be replaced or redirected to a Farmer-local resource container.
    */    
    private bool TryConsumeSeeds(int amount)
    {
        if (amount <= 0)
            return true;

        if (economyService == null)
            economyService = GameManager.Instance;

        return economyService != null && economyService.TrySpendResource(EconomyResource.Seeds, amount);
    }

    /*
    * Directly sets the energy level. This is used for debugging
    * and for any future mechanics that might directly modify
    * energy without going through the normal consumption/regener
    * ation process (e.g. a power-up that instantly refills energy).
    */
    private void SetEnergyLevel(float value)
    {
        runtimeResources.SetEnergy(value);
        PushEnergyToStateSystems();
    }

    /*
    * Directly sets the water level. This is used for debugging
    * and for any future mechanics that might directly modify
    * water without going through the normal consumption/regener
    * ation process (e.g. a power-up that instantly refills water).
    */
    private void SetWaterLevel(float value)
    {
        runtimeResources.SetWater(value);
        PushWaterToStateSystems();
    }

    /*
    * These method push the current resource values to any shared
    * state systems that other parts of the game might be reading.
    */
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
