using System;
using UnityEngine;
using Environment;
using Farming.Crops;
using Farming.Tiles;

namespace Farming.Crops
{
    public enum PlantState
    {
        Planted,
        Growing,
        Mature,
        Withered
    }

    public class Plant : MonoBehaviour
    {
        public const float SproutTimerSeconds = 0f;

        [Header("Crop Definition")]
        [SerializeField] private CropDefinition definition;

        [Header("Runtime State (debug only)")]
        [SerializeField] private string plantState;
        [SerializeField] private float CurrentWater;
        [SerializeField] private float GrowTimeLeft;

        // Runtime lifecycle state
        public PlantState CurrentState { get; private set; }

        // Runtime timers
        [SerializeField] private float growTimer = 0f;
        [SerializeField] private float dryTimerSeconds = 0f;

        // Reference to parent tile
        private FarmTile Tile;

        // True when restored from snapshot
        private bool restoredFromSnapshot;

        // Cached identity (from definition)
        [SerializeField] private string plantName;
        [SerializeField] private int sellValue;

        // Cached growth settings (from definition)
        private float waterNeededToGrow;
        private float growTime;
        private float witherWaterThreshold;
        private float dryOutGraceSeconds;
        private bool regrowsFruit;
        private Season[] growSeasons;

        // Cached visuals (from definition)
        private GameObject plantedModel;
        private GameObject growingModel;
        private GameObject matureModel;
        private GameObject witheredModel;

        public float WaterNeededToGrow => waterNeededToGrow;
        public float GrowTimeSeconds => growTime;
        public float WitherWaterThreshold => witherWaterThreshold;
        public float DryOutGraceSeconds => dryOutGraceSeconds;
        public bool RegrowsFruit => regrowsFruit;

        public PlantData GetHarvestData()
        {
            return new PlantData(plantName, sellValue, 1);
        }

        private void Start()
        {
            // Pull static data from CropDefinition
            plantName = definition.cropName;
            sellValue = definition.sellValue;

            waterNeededToGrow = definition.waterNeededToGrow;
            growTime = definition.growTime;
            witherWaterThreshold = definition.witherWaterThreshold;
            dryOutGraceSeconds = definition.dryOutGraceSeconds;
            regrowsFruit = definition.regrowsFruit;

            growSeasons = definition.growSeasons;

            plantedModel = definition.plantedModel;
            growingModel = definition.growingModel;
            matureModel = definition.matureModel;
            witheredModel = definition.witheredModel;

            SetModelsInactive();

            if (!restoredFromSnapshot)
            {
                SetState(PlantState.Planted);
                growTimer = 0f;
                dryTimerSeconds = 0f;
            }
            else
            {
                UpdateVisuals();
            }

            if (Tile != null)
                Debug.Log("Plant's parent tile: " + Tile.name);
        }

        private void SetModelsInactive()
        {
            if (plantedModel != null) plantedModel.SetActive(false);
            if (growingModel != null) growingModel.SetActive(false);
            if (matureModel != null) matureModel.SetActive(false);
            if (witheredModel != null) witheredModel.SetActive(false);
        }

        private void FixedUpdate()
        {
            plantState = CurrentState.ToString();

            if (Tile == null) return;
            CurrentWater = Tile.GetWater();

            if (CurrentState == PlantState.Withered || CurrentState == PlantState.Mature)
                return;

            float water = Tile.GetWater();

            // Withering logic
            if (water <= witherWaterThreshold)
            {
                dryTimerSeconds += Time.fixedDeltaTime;
                if (dryTimerSeconds >= dryOutGraceSeconds)
                {
                    SetState(PlantState.Withered);
                    Debug.Log("A plant has withered. Water: " + water);
                    return;
                }
            }
            else
            {
                dryTimerSeconds = 0f;
            }

            // Growth logic
            if (water >= waterNeededToGrow)
            {
                if (growTimer >= SproutTimerSeconds && CurrentState == PlantState.Planted)
                {
                    SetState(PlantState.Growing);
                }

                if (CurrentState == PlantState.Growing && growTimer >= growTime)
                {
                    SetState(PlantState.Mature);
                    Tile.TileCondition = FarmTile.Condition.Harvestable;
                    growTimer = 0f;
                    GrowTimeLeft = 0f;
                }

                float growthMultiplier = Tile != null ? Tile.GetGrowthMultiplier() : 1f;
                growTimer += Time.fixedDeltaTime * growthMultiplier;
                GrowTimeLeft = growTime - growTimer;
            }
        }

        public void ResetToDirt()
        {
            Destroy(gameObject);
        }

        public float GetGrowTimer() => growTimer;
        public float GetDryTimer() => dryTimerSeconds;

        public void RestoreFromSnapshot(PlantState state, float restoredGrowTimer, float restoredDryTimer = 0f)
        {
            restoredFromSnapshot = true;
            growTimer = Mathf.Max(0f, restoredGrowTimer);
            dryTimerSeconds = Mathf.Max(0f, restoredDryTimer);
            GrowTimeLeft = Mathf.Max(0f, growTime - growTimer);
            SetModelsInactive();
            SetState(state);
        }

        private void SetState(PlantState newState)
        {
            Debug.Log("Plant changed state: " + newState);
            CurrentState = newState;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (plantedModel != null) plantedModel.SetActive(CurrentState == PlantState.Planted);
            if (growingModel != null) growingModel.SetActive(CurrentState == PlantState.Growing);
            if (matureModel != null) matureModel.SetActive(CurrentState == PlantState.Mature);
            if (witheredModel != null) witheredModel.SetActive(CurrentState == PlantState.Withered);
        }

        internal void SetParentTile(FarmTile farmTile)
        {
            Tile = farmTile;
        }

        bool IsSeasonValid()
        {
            Season current = SeasonManager.Instance.CurrentSeason;

            foreach (Season s in growSeasons)
            {
                if (s == current)
                    return true;
            }

            return false;
        }
    }
}