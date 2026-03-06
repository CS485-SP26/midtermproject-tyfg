using UnityEngine;

namespace Farming
{
    [CreateAssetMenu(fileName = "FarmActorStatsDefinition", menuName = "Farming/Actors/Farm Actor Stats Definition")]
    public class FarmActorStatsDefinition : ScriptableObject
    {
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

        [Header("Interaction")]
        [SerializeField] private int seedsPerPlant = 1;

        public float MaxEnergy => Mathf.Max(1f, maxEnergy);
        public float StartingEnergy => Mathf.Clamp(startingEnergy, 0f, MaxEnergy);
        public float EnergyRegenPerSecond => Mathf.Max(0f, energyRegenPerSecond);
        public float TillEnergyCost => Mathf.Max(0f, tillEnergyCost);
        public float JumpEnergyCost => Mathf.Max(0f, jumpEnergyCost);
        public float SprintEnergyDrainPerSecond => Mathf.Max(0f, sprintEnergyDrainPerSecond);

        public float MaxWater => Mathf.Max(1f, maxWater);
        public float StartingWater => Mathf.Clamp(startingWater, 0f, MaxWater);
        public float WaterPerUse => Mathf.Max(0f, waterPerUse);

        public int SeedsPerPlant => Mathf.Max(1, seedsPerPlant);
    }
}
