namespace Farming
{
    /// <summary>
    /// Shared actor contract for farming interactions.
    /// Player and NPC farmer implementations can use the same interaction pipeline.
    /// </summary>
    public interface IFarmActor
    {
        float TillEnergyCost { get; }
        float WaterPerUse { get; }
        int SeedsPerPlant { get; }
        string LowEnergyMessage { get; }
        string LowWaterMessage { get; }
        string LowSeedsMessage { get; }

        bool TryConsumeEnergyForAction(float amount);
        bool TryConsumeWaterForAction(float amount);
        bool TryConsumeSeedsForAction(int amount);
        void TriggerAnimation(string triggerName);
        void ShowActionBlockedMessage(string message);
    }
}
