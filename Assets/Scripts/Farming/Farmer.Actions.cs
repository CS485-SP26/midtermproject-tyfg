using Farming;
using UnityEngine;
using Farming.Tiles;
using Farming.Crops;
using Farming.SceneState;
using Environment.Tiles;
public partial class Farmer
{
    public void SetTool(string tool)
    {
        toolVisuals.SetTool(tool);
    }

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

        if (runtimeResources.CurrentEnergy <= 0f)
        {
            sprintInputHeld = false;
            movementController.SetSprint(false);
            ShowActionBlockedFeedback(lowEnergyMessage);
        }
    }

    public bool TryConsumeJumpEnergy()
    {
        if (TryConsumeEnergy(jumpEnergyCost))
            return true;

        ShowActionBlockedFeedback(lowEnergyMessage);
        return false;
    }

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

    public void RefillWaterToFull()
    {
        SetWaterLevel(maxWater);
    }

    private void ShowActionBlockedFeedback(string message)
    {
        if (actionFeedback == null)
            actionFeedback = new FarmerActionFeedback(actionFeedbackSettings);

        actionFeedback.TryShow(message);
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
