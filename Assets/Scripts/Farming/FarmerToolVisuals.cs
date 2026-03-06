using UnityEngine;

namespace Farming
{
    [System.Serializable]
    public sealed class FarmerToolVisuals
    {
        [SerializeField] private GameObject wateringCan;
        [SerializeField] private GameObject gardenHoe;

        public void MigrateLegacyIfNeeded(GameObject legacyWateringCan, GameObject legacyGardenHoe)
        {
            if (wateringCan == null)
                wateringCan = legacyWateringCan;

            if (gardenHoe == null)
                gardenHoe = legacyGardenHoe;
        }

        public void SetTool(string toolName)
        {
            if (wateringCan != null)
                wateringCan.SetActive(false);

            if (gardenHoe != null)
                gardenHoe.SetActive(false);

            switch (toolName)
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
    }
}
