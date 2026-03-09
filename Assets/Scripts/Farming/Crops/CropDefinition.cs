using UnityEngine;

namespace Farming.Crops
{
    [CreateAssetMenu(menuName = "Farming/Crop Definition")]
    public class CropDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Display name of the crop (e.g., Turnip, Carrot, Wheat).")]
        public string cropName = "Turnip";

        [Tooltip("How much money the player earns when harvesting this crop.")]
        public int sellValue = 10;

        [Tooltip("How many seeds are required to plant this crop.")]
        public int seedCost = 1;

        [Header("Growth Settings")]
        [Tooltip("Water required before the plant transitions from Planted → Growing.")]
        public float waterNeededToGrow = 5f;

        [Tooltip("Time (seconds) spent in Growing state before becoming Mature.")]
        public float growTime = 1f;

        [Tooltip("If water falls below this threshold, the plant begins drying out.")]
        public float witherWaterThreshold = 0.1f;

        [Tooltip("How long the plant can stay dry before withering.")]
        public float dryOutGraceSeconds = 60f;

        [Tooltip("If true, the plant will regrow fruit after harvesting.")]
        public bool regrowsFruit = false;

        [Header("Seasons")]
        [Tooltip("Which seasons this crop is allowed to grow in.")]
        public Season[] growSeasons;

        [Header("Visual Prefabs")]
        [Tooltip("Model shown when the seed is first planted.")]
        public GameObject plantedModel;

        [Tooltip("Model shown while the plant is growing.")]
        public GameObject growingModel;

        [Tooltip("Model shown when the plant is fully mature.")]
        public GameObject matureModel;

        [Tooltip("Model shown when the plant has withered.")]
        public GameObject witheredModel;
    }
}