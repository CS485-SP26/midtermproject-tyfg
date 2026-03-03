using System.Collections.Generic;
using UnityEngine;
using Core;
using UnityEngine.SceneManagement;
using Environment;

/*
* This class represents a single tile in the farm. It manages its own state (grass, tilled, watered, planted) and handles interactions 
    such as tilling, watering, and planting seeds.
* It also handles the visual representation of the tile based on its state and plays appropriate audio cues for interactions.
* The tile can be highlighted when selected, and it tracks the number of days since the last interaction to determine if it should revert 
    to a less cultivated state (e.g., watered -> tilled -> grass).
* Exposes:
*   - GetCondition: A property to get the current condition of the tile.
*   - Interact(): A method to interact with the tile, which will perform an action based on its current state (till, water, plant).
*   - OnDayPassed(): A method that should be called when a day passes in the game, which will update the tile's state based on how long it's been since the last interaction.
* Requires:
*   - A MeshRenderer component for visual representation.
*   - AudioSource components for step, tilling, and watering sounds.
*/

namespace Farming 
{
    [RequireComponent(typeof(Transform))]
    public class FarmTile : MonoBehaviour
    {
        private const int FallbackAllTilesRewardFunds = 25;
        private const float WitherWaterThreshold = 0.1f;

        public enum Condition { Grass, Tilled, Watered, Planted, Harvestable }

        [SerializeField] private Condition tileCondition = Condition.Grass; 
        // Continuous water loss over time.
        [SerializeField] private float waterDecayPerSecond = 0.1f;
        [SerializeField] private GameObject plantPrefab;

        // Runtime plant instance currently occupying this tile (if any).
        private Plant currentPlant;
        
        [Header("Data")]
        private float waterAmount = 5f;

        [Header("Visuals")]
        [SerializeField] private Material grassMaterial;
        [SerializeField] private Material tilledMaterial;
        [SerializeField] private Material wateredMaterial;
        MeshRenderer tileRenderer;

        [Header("Audio")]
        [SerializeField] private AudioSource stepAudio;
        [SerializeField] private AudioSource tillAudio;
        [SerializeField] private AudioSource waterAudio;

        List<Material> materials = new List<Material>();
        private float currentWater = 0f;
        private string persistenceKey;

        private int daysSinceLastInteraction = 0;
        public FarmTile.Condition TileCondition
        {
            get { return tileCondition; }
            set // Used in Plant class to communicate when plant is mature
            {
                tileCondition = value;
            }
        }

        // Builds a stable key used to persist this tile's runtime state.
        private void Awake()
        {
            persistenceKey = BuildPersistenceKey();
        }

        // Caches renderer references and highlight materials.
        void Start()
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
            waterDecayPerSecond *= .02f; // hopefully accounts for FixedUpdate frequency
            TryRestorePersistedState();
        }

        // Persists latest state before this scene unloads.
        private void OnDisable()
        {
            SavePersistedState();
        }

        // Persists latest state on destroy as a fallback path.
        private void OnDestroy()
        {
            SavePersistedState();
        }

        private void FixedUpdate()
        {
            if (currentWater > 0)
            {
                currentWater = Mathf.Max(0f, currentWater - waterDecayPerSecond);
            }
        }

        // Primary interaction state machine for till/water/plant progression.
        public void Interact()
        {
            switch(tileCondition)
            {
                case FarmTile.Condition.Grass: Till(); break;
                case FarmTile.Condition.Tilled: Water(); break;
                case FarmTile.Condition.Watered: PlantSeed(); break;
                case FarmTile.Condition.Planted: Water(); break;
                case FarmTile.Condition.Harvestable:
                {
                    HarvestPlant(); // Runs regardless of whether plant can regrow fruit
                    if (currentPlant == null || !currentPlant.RegrowsFruit) // Only remove plant if it can't regrow
                    {
                        ClearPlant();
                        Till();
                    }
                } break;
            }
            daysSinceLastInteraction = 0;
            SavePersistedState();
            FarmWinController.NotifyTileStatePotentiallyChanged();
            EvaluateAllTilesRewardFallback();
        }

        // Transitions tile to tilled state and refreshes visuals/audio.
        public void Till()
        {
            tileCondition = FarmTile.Condition.Tilled;
            UpdateVisual();
            tillAudio?.Play();
        }

        // Waters planted crop if present; otherwise waters bare tilled soil.
        public void Water()
        {
            if (tileCondition == Condition.Grass) return; // Can't water grass

            // Tile condition only updates on Tilled. Prevents overwriting Planted condition.
            if (tileCondition == Condition.Tilled)
            {
                tileCondition = Condition.Watered;
                UpdateVisual();
            }

            currentWater += waterAmount;
            waterAudio?.Play();
            return;
        }

        public float GetWater()
        {
            return currentWater;
        }
        // TODO: Check if we need to destroy plantObj at any point
        GameObject plantObj;

        // Spawns plant prefab and transitions tile into planted state.
        private void PlantSeed()
        {
            if (currentPlant != null)
                return;

            EnsurePlantExists();
            if (currentPlant == null)
                return;

            tileCondition = Condition.Planted;
            plantObj.SetActive(true);
            Debug.Log("Plant active? " + plantObj.activeInHierarchy);
            UpdateVisual();
        }
        
        // TODO: Take the harvested plant object and store its data in inventory (to be implemented)
        private void HarvestPlant()
        {
            // I think we need to store DEEP copies of relevant data from the currentPlant (the Plant component of plantObj)
            // because currentPlant will be destroyed when harvested.
            // Idea: consider making a data structure for storing plant data, basically separating the Plant class into 
            // two parts, one for holding data, the other for manipulating that data. 
            // Separation of concerns or something like that idk. I think it'd feel more organized.
        }
        
        // Clears existing plant and resets tile to grass state.
        private void ClearPlant()
        {
            if (currentPlant != null)
                Destroy(currentPlant.gameObject);

            currentPlant = null;
            plantObj = null;
            tileCondition = Condition.Grass;
            UpdateVisual();
        }

        // Ensures a runtime plant instance exists and is parented to this tile.
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

        // Applies material based on current tile condition.
        private void UpdateVisual()
        {
            if(tileRenderer == null) return;
            switch(tileCondition)
            {
                case FarmTile.Condition.Grass: tileRenderer.material = grassMaterial; break;
                case FarmTile.Condition.Tilled: tileRenderer.material = tilledMaterial; break;
                case FarmTile.Condition.Watered: tileRenderer.material = wateredMaterial; break;
            }
        }

        // Toggles emissive highlight on tile border materials.
        public void SetHighlight(bool active)
        {
            foreach (Material m in materials)
            {
                if (active)
                {
                    m.EnableKeyword("_EMISSION");
                } 
                else 
                {
                    m.DisableKeyword("_EMISSION");
                }
            }
            if (active) stepAudio.Play();
        }

        // Day tick handler for decay/wither behavior and win-state refresh.
        public void OnDayPassed()
        {
            ApplyDayPassedLogic(true);
        }

        // Applies one full day-tick of tile decay/wither behavior.
        private void ApplyDayPassedLogic(bool persistState)
        {
            Condition previousCondition = tileCondition;
            daysSinceLastInteraction++;
            if (tileCondition == Condition.Planted && currentPlant != null)
            {
                if (currentPlant.CurrentState == PlantState.Withered)
                {
                    ClearPlant();
                }
            }

            if(daysSinceLastInteraction >= 2) // TODO: Consider making this a [SerializeField]
            {
                if(tileCondition == FarmTile.Condition.Watered) tileCondition = FarmTile.Condition.Tilled;
                else if(tileCondition == FarmTile.Condition.Tilled) tileCondition = FarmTile.Condition.Grass;
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

        // Serializes current tile/plant state into the cross-scene farm cache.
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
                SavedAtRealtimeSeconds = Time.realtimeSinceStartup
            };

            FarmSceneStateStore.SaveTileState(persistenceKey, snapshot);
        }

        // Restores persisted state and advances simulation by elapsed off-scene time.
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

        // Applies a snapshot without elapsed-time simulation.
        private void ApplySnapshot(FarmSceneStateStore.FarmTileSnapshot snapshot)
        {
            tileCondition = snapshot.TileCondition;
            currentWater = Mathf.Max(0f, snapshot.WaterAmount);
            daysSinceLastInteraction = Mathf.Max(0, snapshot.DaysSinceLastInteraction);

            bool shouldHavePlant = snapshot.HasPlant || tileCondition == Condition.Planted || tileCondition == Condition.Harvestable;
            if (shouldHavePlant)
            {
                EnsurePlantExists();
                if (currentPlant != null)
                {
                    currentPlant.SetParentTile(this);
                    currentPlant.RestoreFromSnapshot(snapshot.PlantState, snapshot.PlantGrowTimer);
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

            if (state != PlantState.Mature && state != PlantState.Withered)
            {
                float timeUntilWitherSeconds;
                if (initialWater <= WitherWaterThreshold)
                {
                    timeUntilWitherSeconds = 0f;
                }
                else if (waterDecayRatePerSecond <= 0f)
                {
                    timeUntilWitherSeconds = float.PositiveInfinity;
                }
                else
                {
                    timeUntilWitherSeconds = (initialWater - WitherWaterThreshold) / waterDecayRatePerSecond;
                }

                float growthSimulationWindow = Mathf.Min(elapsedSeconds, Mathf.Max(0f, timeUntilWitherSeconds));
                float growthWindowSeconds = GetDurationAtOrAboveWaterThreshold(
                    initialWater,
                    waterDecayRatePerSecond,
                    currentPlant.WaterNeededToGrow,
                    growthSimulationWindow);

                growTimer += Mathf.Max(0f, growthWindowSeconds);

                if (state == PlantState.Planted && growTimer >= Plant.SproutTimerSeconds)
                    state = PlantState.Growing;

                if (state == PlantState.Growing && growTimer >= currentPlant.GrowTimeSeconds)
                {
                    state = PlantState.Mature;
                    tileCondition = Condition.Harvestable;
                    growTimer = 0f;
                }
                else if (timeUntilWitherSeconds <= elapsedSeconds)
                {
                    state = PlantState.Withered;
                }
            }

            currentPlant.RestoreFromSnapshot(state, growTimer);
            if (state == PlantState.Mature)
                tileCondition = Condition.Harvestable;

            UpdateVisual();
            FarmWinController.NotifyTileStatePotentiallyChanged();
        }

        // Computes how long water stayed above a threshold while decaying linearly.
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

        // Converts per-fixed-step decay back into per-second decay rate.
        private float GetWaterDecayRatePerSecond()
        {
            float fixedStep = Mathf.Max(0.001f, Time.fixedDeltaTime);
            return waterDecayPerSecond / fixedStep;
        }

        // Builds a deterministic runtime key for this tile in its scene.
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

        // Legacy fallback reward evaluation when all non-purchase tiles are watered.
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

                if (tile.GetComponent<SeedPurchaseTile>() != null)
                    continue;

                foundAnyFarmableTile = true;
                if (tile.TileCondition != FarmTile.Condition.Watered)
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
