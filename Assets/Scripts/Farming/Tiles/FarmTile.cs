using System.Collections.Generic;
using UnityEngine;
using Core;
using UnityEngine.SceneManagement;
using Environment;
using Farming.Crops;   // NEW — for Plant, PlantState, PlantData
using Farming.SceneState;   // NEW — for FarmSceneStateStore

namespace Farming.Tiles
{
    [RequireComponent(typeof(Transform))]
    public class FarmTile : MonoBehaviour
    {
        private const int FallbackAllTilesRewardFunds = 25;

        public enum Condition { Grass, Tilled, Watered, Planted, Harvestable }

        [SerializeField] private Condition tileCondition = Condition.Grass;
        [SerializeField] private FarmTileDefinition tileDefinition;

        // Continuous water loss over time.
        [SerializeField] private float waterDecayPerSecond = 0.1f;

        // TODO: This will eventually be replaced by CropDefinition.prefab
        [SerializeField] private GameObject plantPrefab;

        // Runtime plant instance currently occupying this tile (if any).
        private Plant currentPlant;

        [Header("Data")]
        private float waterAmount = 5f;
        [SerializeField] private bool clearWitheredPlantOnDayPassed = false;

        [Header("Visuals")]
        [SerializeField] private Material grassMaterial;
        [SerializeField] private Material tilledMaterial;
        [SerializeField] private Material wateredMaterial;

        private MeshRenderer tileRenderer;

        [Header("Audio")]
        [SerializeField] private AudioSource stepAudio;
        [SerializeField] private AudioSource tillAudio;
        [SerializeField] private AudioSource waterAudio;

        private readonly List<Material> materials = new List<Material>();
        private float currentWater = 0f;
        private string persistenceKey;

        private GameObject plantObj;

        private int daysSinceLastInteraction = 0;

        public Condition TileCondition
        {
            get => tileCondition;
            set => tileCondition = value; // Used by Plant when it becomes mature
        }

        public FarmTileDefinition TileDefinition => tileDefinition;

        // True if this tile should participate in all-tiles-watered reward checks.
        public bool CountsForWaterReward()
        {
            if (tileDefinition != null)
                return tileDefinition.CountsForWaterReward;

            // Backwards-compatible default for scenes that haven't assigned definitions yet.
            return GetComponent<SeedPurchaseTile>() == null;
        }

        // True if tile currently qualifies as watered for reward purposes.
        public bool IsWateredForReward()
        {
            if (tileCondition == Condition.Watered)
                return true;

            float threshold = tileDefinition != null ? tileDefinition.RewardWaterThreshold : 0.1f;
            bool plantedOrHarvestable = tileCondition == Condition.Planted || tileCondition == Condition.Harvestable;

            return plantedOrHarvestable && currentWater > threshold;
        }

        public float GetGrowthMultiplier()
        {
            return tileDefinition != null ? tileDefinition.GrowthMultiplier : 1f;
        }

        public bool SupportsTilling()
        {
            return tileDefinition == null || tileDefinition.SupportsTilling;
        }

        public bool SupportsWatering()
        {
            return tileDefinition == null || tileDefinition.SupportsWatering;
        }

        public bool SupportsPlanting()
        {
            return tileDefinition == null || tileDefinition.SupportsPlanting;
        }

        // Builds a stable key used to persist this tile's runtime state.
        private void Awake()
        {
            persistenceKey = BuildPersistenceKey();
        }

        // Caches renderer references and highlight materials.
        private void Start()
        {
            tileRenderer = GetComponent<MeshRenderer>();
            Debug.Assert(tileRenderer, "FarmTile requires a MeshRenderer");

            foreach (Transform edge in transform)
            {
                MeshRenderer edgeRenderer = edge.gameObject.GetComponent<MeshRenderer>();
                if (edgeRenderer != null)
                    materials.Add(edgeRenderer.material);
            }

            currentWater = 0f;
            waterDecayPerSecond *= 0.02f; // Adjust for FixedUpdate frequency

            TryRestorePersistedState();
        }

        private void OnEnable()
        {
            DayController.DayAdvanced -= HandleDayAdvanced;
            DayController.DayAdvanced += HandleDayAdvanced;
        }

        private void OnDisable()
        {
            DayController.DayAdvanced -= HandleDayAdvanced;
            SavePersistedState();
        }

        private void OnDestroy()
        {
            DayController.DayAdvanced -= HandleDayAdvanced;
            SavePersistedState();
        }

        private void FixedUpdate()
        {
            if (currentWater > 0)
            {
                currentWater = Mathf.Max(0f, currentWater - (waterDecayPerSecond * GetWaterDecayMultiplier()));

                if (tileCondition == Condition.Planted || tileCondition == Condition.Harvestable)
                    UpdateVisual();
            }
        }

        // Transitions tile to tilled state and refreshes visuals/audio.
        private void Till()
        {
            if (!SupportsTilling())
                return;

            tileCondition = Condition.Tilled;
            UpdateVisual();
            tillAudio?.Play();
        }

        // Waters planted crop if present; otherwise waters bare tilled soil.
        private void Water()
        {
            if (!SupportsWatering())
                return;

            if (tileCondition == Condition.Grass)
                return; // Can't water grass

            // Tile condition only updates on Tilled. Prevents overwriting Planted condition.
            if (tileCondition == Condition.Tilled)
            {
                tileCondition = Condition.Watered;
                UpdateVisual();
            }

            currentWater += waterAmount;

            if (tileCondition == Condition.Planted || tileCondition == Condition.Harvestable)
                UpdateVisual();

            waterAudio?.Play();
        }

        // Primary interaction state machine for till/water/plant progression.
        public void Interact()
        {
            Debug.Log("Interacted with tile in condition: " + tileCondition);

            switch (tileCondition)
            {
                case Condition.Grass:
                    Till();
                    break;

                case Condition.Tilled:
                    Water();
                    break;

                case Condition.Watered:
                    PlantSeed();
                    break;

                case Condition.Planted:
                    if (currentPlant != null && currentPlant.CurrentState == PlantState.Withered)
                    {
                        ClearPlant();
                        Till();
                        break;
                    }
                    Water();
                    break;

                case Condition.Harvestable:
                    HarvestPlant();
                    if (currentPlant == null || !currentPlant.RegrowsFruit)
                    {
                        ClearPlant();
                        Till();
                    }
                    break;
            }

            daysSinceLastInteraction = 0;
            SavePersistedState();
            FarmWinController.NotifyTileStatePotentiallyChanged();
            EvaluateAllTilesRewardFallback();
        }

                private void PlantSeed()
        {
            if (!SupportsPlanting())
                return;

            if (currentPlant != null)
                return;

            // Keep newly planted soil visibly moist so it does not appear instantly dry.
            if (currentWater <= 0.1f)
                currentWater = waterAmount;

            EnsurePlantExists();
            if (currentPlant == null)
                return;

            tileCondition = Condition.Planted;
            plantObj.SetActive(true);
            Debug.Log("Plant active? " + plantObj.activeInHierarchy);
            UpdateVisual();
        }

        private void HarvestPlant()
        {
            if (currentPlant == null)
                return;

            if (currentPlant.CurrentState != PlantState.Mature)
                return;

            // Extract only needed data
            PlantData harvestedData = currentPlant.GetHarvestData();

            // TODO: Send to inventory system
            Debug.Log("Harvested: " + harvestedData.plantName + " added to inventory");

            // Temporary economy hook
            GameManager.Instance.AddResource(EconomyResource.Plants, 1);

            // Remove plant from tile
            Destroy(currentPlant.gameObject);
            currentPlant = null;
            tileCondition = Condition.Grass;

            UpdateVisual();
        }

        private void ClearPlant()
        {
            if (currentPlant != null)
                Destroy(currentPlant.gameObject);

            currentPlant = null;
            plantObj = null;
            tileCondition = Condition.Grass;
            UpdateVisual();
        }

        private void EnsurePlantExists()
        {
            if (currentPlant != null)
                return;

            if (plantPrefab == null)
                return;

            plantObj = Instantiate(plantPrefab, transform.position, Quaternion.identity);
            currentPlant = plantObj.GetComponent<Plant>();

            if (currentPlant != null)
                currentPlant.SetParentTile(this);
        }

        private void UpdateVisual()
        {
            if (tileRenderer == null)
                return;

            switch (tileCondition)
            {
                case Condition.Grass:
                    tileRenderer.material = grassMaterial;
                    break;

                case Condition.Tilled:
                    tileRenderer.material = tilledMaterial;
                    break;

                case Condition.Watered:
                    tileRenderer.material = wateredMaterial;
                    break;

                case Condition.Planted:
                case Condition.Harvestable:
                    tileRenderer.material = currentWater > 0.1f ? wateredMaterial : tilledMaterial;
                    break;
            }
        }

        public void SetHighlight(bool active)
        {
            foreach (Material m in materials)
            {
                if (active)
                    m.EnableKeyword("_EMISSION");
                else
                    m.DisableKeyword("_EMISSION");
            }

            if (active)
                stepAudio?.Play();
        }

        public void OnDayPassed()
        {
            ApplyDayPassedLogic(true);
        }

        private void HandleDayAdvanced(int dayNumber)
        {
            ApplyDayPassedLogic(true);
        }

        private void ApplyDayPassedLogic(bool persistState)
        {
            Condition previousCondition = tileCondition;
            daysSinceLastInteraction++;

            if (clearWitheredPlantOnDayPassed &&
                tileCondition == Condition.Planted &&
                currentPlant != null &&
                currentPlant.CurrentState == PlantState.Withered)
            {
                ClearPlant();
            }

            if (daysSinceLastInteraction >= 2)
            {
                if (tileCondition == Condition.Watered)
                    tileCondition = Condition.Tilled;
                else if (tileCondition == Condition.Tilled)
                    tileCondition = Condition.Grass;
            }

            UpdateVisual();

            if (previousCondition != tileCondition)
            {
                FarmWinController.NotifyTileStatePotentiallyChanged();
                EvaluateAllTilesRewardFallback();
            }

            if (persistState)
                SavePersistedState();
        }

        private void SavePersistedState()
        {
            if (!Application.isPlaying)
                return;

            if (string.IsNullOrWhiteSpace(persistenceKey))
                persistenceKey = BuildPersistenceKey();

            FarmSceneStateStore.FarmTileSnapshot snapshot = new FarmSceneStateStore.FarmTileSnapshot
            {
                TileCondition = tileCondition,
                WaterAmount = Mathf.Max(0f, currentWater),
                DaysSinceLastInteraction = Mathf.Max(0, daysSinceLastInteraction),
                HasPlant = currentPlant != null,
                PlantState = currentPlant != null ? currentPlant.CurrentState : PlantState.Planted,
                PlantGrowTimer = currentPlant != null ? currentPlant.GetGrowTimer() : 0f,
                PlantDryTimer = currentPlant != null ? currentPlant.GetDryTimer() : 0f,
                SavedAtRealtimeSeconds = Time.realtimeSinceStartup
            };

            FarmSceneStateStore.SaveTileState(persistenceKey, snapshot);
        }

        private void TryRestorePersistedState()
        {
            if (!Application.isPlaying)
                return;

            if (string.IsNullOrWhiteSpace(persistenceKey))
                persistenceKey = BuildPersistenceKey();

            if (!FarmSceneStateStore.TryGetTileState(persistenceKey, out FarmSceneStateStore.FarmTileSnapshot snapshot))
                return;

            ApplySnapshot(snapshot);

            float elapsedSeconds = Mathf.Max(0f, Time.realtimeSinceStartup - snapshot.SavedAtRealtimeSeconds);
            if (elapsedSeconds > 0f)
                SimulateElapsedOffSceneTime(elapsedSeconds);

            SavePersistedState();
        }

        public float GetWater()
        {
            return currentWater;
        }

        private void ApplySnapshot(FarmSceneStateStore.FarmTileSnapshot snapshot)
        {
            tileCondition = snapshot.TileCondition;
            currentWater = Mathf.Max(0f, snapshot.WaterAmount);
            daysSinceLastInteraction = Mathf.Max(0, snapshot.DaysSinceLastInteraction);

            bool shouldHavePlant =
                snapshot.HasPlant ||
                tileCondition == Condition.Planted ||
                tileCondition == Condition.Harvestable;

            if (shouldHavePlant)
            {
                EnsurePlantExists();

                if (currentPlant != null)
                {
                    currentPlant.SetParentTile(this);
                    currentPlant.RestoreFromSnapshot(snapshot.PlantState, snapshot.PlantGrowTimer, snapshot.PlantDryTimer);
                }
            }
            else if (currentPlant != null)
            {
                Destroy(currentPlant.gameObject);
                currentPlant = null;
                plantObj = null;
            }

            UpdateVisual();
            FarmWinController.NotifyTileStatePotentiallyChanged();
        }

                // Simulates growth/decay while this tile's scene was unloaded.
        private void SimulateElapsedOffSceneTime(float elapsedSeconds)
        {
            float waterDecayRatePerSecond = GetWaterDecayRatePerSecond();
            float initialWater = currentWater;
            float finalWater = Mathf.Max(0f, initialWater - (waterDecayRatePerSecond * elapsedSeconds));
            currentWater = finalWater;

            int elapsedWholeDays = Mathf.FloorToInt(elapsedSeconds / Mathf.Max(1f, DayController.RuntimeDayLengthSeconds));
            if (elapsedWholeDays > 0)
            {
                for (int i = 0; i < elapsedWholeDays; i++)
                    ApplyDayPassedLogic(false);
            }

            if (currentPlant == null)
                return;

            PlantState state = currentPlant.CurrentState;
            float growTimer = currentPlant.GetGrowTimer();
            float dryTimer = currentPlant.GetDryTimer();
            float witherWaterThreshold = currentPlant.WitherWaterThreshold;
            float dryOutGraceSeconds = Mathf.Max(0f, currentPlant.DryOutGraceSeconds);

            if (state != PlantState.Mature && state != PlantState.Withered)
            {
                float dryDurationSeconds = GetDurationAtOrBelowWaterThreshold(
                    initialWater,
                    waterDecayRatePerSecond,
                    witherWaterThreshold,
                    elapsedSeconds);

                dryTimer += Mathf.Max(0f, dryDurationSeconds);

                float growthWindowSeconds = GetDurationAtOrAboveWaterThreshold(
                    initialWater,
                    waterDecayRatePerSecond,
                    currentPlant.WaterNeededToGrow,
                    elapsedSeconds);

                growTimer += Mathf.Max(0f, growthWindowSeconds) * GetGrowthMultiplier();

                if (state == PlantState.Planted && growTimer >= Plant.SproutTimerSeconds)
                    state = PlantState.Growing;

                if (state == PlantState.Growing && growTimer >= currentPlant.GrowTimeSeconds)
                {
                    state = PlantState.Mature;
                    tileCondition = Condition.Harvestable;
                    growTimer = 0f;
                    dryTimer = 0f;
                }
                else if (dryTimer >= dryOutGraceSeconds)
                {
                    state = PlantState.Withered;
                }
            }

            currentPlant.RestoreFromSnapshot(state, growTimer, dryTimer);

            if (state == PlantState.Mature)
                tileCondition = Condition.Harvestable;

            UpdateVisual();
            FarmWinController.NotifyTileStatePotentiallyChanged();
        }

        private static float GetDurationAtOrAboveWaterThreshold(
            float initialWater,
            float decayRatePerSecond,
            float threshold,
            float maxDurationSeconds)
        {
            if (maxDurationSeconds <= 0f)
                return 0f;

            if (initialWater < threshold)
                return 0f;

            if (decayRatePerSecond <= 0f)
                return maxDurationSeconds;

            float secondsUntilDropBelowThreshold = (initialWater - threshold) / decayRatePerSecond;
            return Mathf.Clamp(secondsUntilDropBelowThreshold, 0f, maxDurationSeconds);
        }

        private static float GetDurationAtOrBelowWaterThreshold(
            float initialWater,
            float decayRatePerSecond,
            float threshold,
            float maxDurationSeconds)
        {
            if (maxDurationSeconds <= 0f)
                return 0f;

            if (initialWater <= threshold)
                return maxDurationSeconds;

            if (decayRatePerSecond <= 0f)
                return 0f;

            float secondsUntilDropToThreshold = (initialWater - threshold) / decayRatePerSecond;

            if (secondsUntilDropToThreshold >= maxDurationSeconds)
                return 0f;

            return maxDurationSeconds - Mathf.Max(0f, secondsUntilDropToThreshold);
        }

        private float GetWaterDecayRatePerSecond()
        {
            float fixedStep = Mathf.Max(0.001f, Time.fixedDeltaTime);
            return (waterDecayPerSecond * GetWaterDecayMultiplier()) / fixedStep;
        }

        private float GetWaterDecayMultiplier()
        {
            return tileDefinition != null ? tileDefinition.WaterDecayMultiplier : 1f;
        }

        private string BuildPersistenceKey()
        {
            Scene scene = gameObject.scene;
            string sceneName = scene.IsValid() ? scene.name : "UnknownScene";

            Vector3 position = transform.position;
            int x = Mathf.RoundToInt(position.x * 100f);
            int y = Mathf.RoundToInt(position.y * 100f);
            int z = Mathf.RoundToInt(position.z * 100f);

            return sceneName + "|" + gameObject.name + "|" + x + "," + y + "," + z;
        }

        private static void EvaluateAllTilesRewardFallback()
        {
            FarmTile[] tiles = FindObjectsByType<FarmTile>(FindObjectsSortMode.None);
            if (tiles == null || tiles.Length == 0)
                return;

            bool foundAnyFarmableTile = false;
            bool allWatered = true;

            foreach (FarmTile tile in tiles)
            {
                if (tile == null)
                    continue;

                if (!tile.CountsForWaterReward())
                    continue;

                foundAnyFarmableTile = true;

                if (!tile.IsWateredForReward())
                {
                    allWatered = false;
                    break;
                }
            }

            if (!foundAnyFarmableTile)
                return;

            GameManager gameManager = GameManager.Instance;

            if (allWatered)
            {
                if (!gameManager.IsFlagSet(FarmWinController.AllTilesRewardGivenFlag))
                {
                    gameManager.AddFunds(FallbackAllTilesRewardFunds);
                    gameManager.SetFlag(FarmWinController.AllTilesRewardGivenFlag, true);
                }
            }
            else
            {
                gameManager.SetFlag(FarmWinController.AllTilesRewardGivenFlag, false);
            }
        }
    }
}