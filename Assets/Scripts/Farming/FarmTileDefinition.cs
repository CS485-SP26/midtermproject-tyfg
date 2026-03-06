using UnityEngine;

namespace Farming
{
    [CreateAssetMenu(fileName = "FarmTileDefinition", menuName = "Farming/Tiles/Farm Tile Definition")]
    public class FarmTileDefinition : TileDefinition
    {
        public enum SoilProfile
        {
            Normal,
            Soft,
            Rough,
            Custom
        }

        [Header("Capabilities")]
        [SerializeField] private bool countsForWaterReward = true;
        [SerializeField] private bool supportsTilling = true;
        [SerializeField] private bool supportsWatering = true;
        [SerializeField] private bool supportsPlanting = true;

        [Header("Soil")]
        [SerializeField] private SoilProfile soilProfile = SoilProfile.Normal;
        [SerializeField] private float customGrowthMultiplier = 1f;
        [SerializeField] private float customWaterDecayMultiplier = 1f;
        [SerializeField] private float rewardWaterThreshold = 0.1f;

        public bool CountsForWaterReward => countsForWaterReward;
        public bool SupportsTilling => supportsTilling;
        public bool SupportsWatering => supportsWatering;
        public bool SupportsPlanting => supportsPlanting;
        public float RewardWaterThreshold => Mathf.Max(0f, rewardWaterThreshold);

        public float GrowthMultiplier
        {
            get
            {
                switch (soilProfile)
                {
                    case SoilProfile.Soft:
                        return 1.2f;
                    case SoilProfile.Rough:
                        return 0.75f;
                    case SoilProfile.Custom:
                        return Mathf.Max(0.01f, customGrowthMultiplier);
                    default:
                        return 1f;
                }
            }
        }

        public float WaterDecayMultiplier
        {
            get
            {
                switch (soilProfile)
                {
                    case SoilProfile.Soft:
                        return 0.8f;
                    case SoilProfile.Rough:
                        return 1.35f;
                    case SoilProfile.Custom:
                        return Mathf.Max(0f, customWaterDecayMultiplier);
                    default:
                        return 1f;
                }
            }
        }
    }
}
