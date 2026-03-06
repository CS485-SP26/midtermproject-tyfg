using UnityEngine;

namespace Farming
{
    // Owns runtime energy/water values and mutation rules for a farm actor.
    public sealed class FarmerRuntimeResources
    {
        private float maxEnergy = 100f;
        private float maxWater = 100f;
        private float energyRegenPerSecond = 8f;
        private float currentEnergy;
        private float currentWater;

        public float CurrentEnergy => currentEnergy;
        public float CurrentWater => currentWater;

        public void Configure(float maxEnergyValue, float maxWaterValue, float regenPerSecond)
        {
            maxEnergy = Mathf.Max(1f, maxEnergyValue);
            maxWater = Mathf.Max(1f, maxWaterValue);
            energyRegenPerSecond = Mathf.Max(0f, regenPerSecond);
            currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);
            currentWater = Mathf.Clamp(currentWater, 0f, maxWater);
        }

        public void SetEnergy(float value)
        {
            currentEnergy = Mathf.Clamp(value, 0f, maxEnergy);
        }

        public void SetWater(float value)
        {
            currentWater = Mathf.Clamp(value, 0f, maxWater);
        }

        public bool TryConsumeEnergy(float amount)
        {
            if (amount <= 0f)
                return true;

            if (currentEnergy + 0.001f < amount)
                return false;

            currentEnergy = Mathf.Clamp(currentEnergy - amount, 0f, maxEnergy);
            return true;
        }

        public bool TryConsumeWater(float amount)
        {
            if (amount <= 0f)
                return true;

            if (currentWater + 0.001f < amount)
                return false;

            currentWater = Mathf.Clamp(currentWater - amount, 0f, maxWater);
            return true;
        }

        public bool TryRegenerateEnergy(bool isActivelySprinting, float deltaTime)
        {
            if (isActivelySprinting || energyRegenPerSecond <= 0f || currentEnergy >= maxEnergy)
                return false;

            currentEnergy = Mathf.Clamp(currentEnergy + (energyRegenPerSecond * deltaTime), 0f, maxEnergy);
            return true;
        }
    }
}
