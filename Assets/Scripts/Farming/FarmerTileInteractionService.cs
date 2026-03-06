namespace Farming
{
    /// <summary>
    /// Encapsulates tile interaction rules so Farmer/NPC controllers can share logic.
    /// </summary>
    public sealed class FarmerTileInteractionService
    {
        public void TryInteract(IFarmActor actor, FarmTile tile)
        {
            if (actor == null || tile == null)
                return;

            switch (tile.TileCondition)
            {
                case FarmTile.Condition.Grass:
                    if (!tile.SupportsTilling())
                        return;

                    if (!actor.TryConsumeEnergyForAction(actor.TillEnergyCost))
                    {
                        actor.ShowActionBlockedMessage(actor.LowEnergyMessage);
                        return;
                    }

                    actor.TriggerAnimation("Till");
                    tile.Interact();
                    break;

                case FarmTile.Condition.Tilled:
                    if (!tile.SupportsWatering())
                        return;

                    if (!actor.TryConsumeWaterForAction(actor.WaterPerUse))
                    {
                        actor.ShowActionBlockedMessage(actor.LowWaterMessage);
                        return;
                    }

                    actor.TriggerAnimation("Water");
                    tile.Interact();
                    break;

                case FarmTile.Condition.Watered:
                    if (!tile.SupportsPlanting())
                        return;

                    if (!actor.TryConsumeSeedsForAction(actor.SeedsPerPlant))
                    {
                        actor.ShowActionBlockedMessage(actor.LowSeedsMessage);
                        return;
                    }

                    tile.Interact();
                    break;

                case FarmTile.Condition.Planted:
                    if (!tile.SupportsWatering())
                        return;

                    if (!actor.TryConsumeWaterForAction(actor.WaterPerUse))
                    {
                        actor.ShowActionBlockedMessage(actor.LowWaterMessage);
                        return;
                    }

                    actor.TriggerAnimation("Water");
                    tile.Interact();
                    break;

                case FarmTile.Condition.Harvestable:
                    tile.Interact();
                    break;
            }
        }
    }
}
