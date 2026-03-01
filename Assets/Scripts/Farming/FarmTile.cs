using System.Collections.Generic;
using UnityEngine;
using Environment;
using Core;
using UnityEngine.UIElements;
using UnityEngine.Tilemaps;

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

        public enum Condition { Grass, Tilled, Watered, Planted, Harvestable }

        [SerializeField] private Condition tileCondition = Condition.Grass; 
        // Continuous water loss over time.
        [SerializeField] private float waterDecayPerSecond = 0.1f;
        [SerializeField] private GameObject plantPrefab;

        private Vector3 plantSpawnPointPos; // Set in Start()

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

        private int daysSinceLastInteraction = 0;
        public FarmTile.Condition TileCondition
        {
            get { return tileCondition; }
            set // Used in Plant class to communicate when plant is mature
            {
                tileCondition = value;
            }
        }

        // Caches renderer references and highlight materials.
        void Start()
        {
            tileRenderer = GetComponent<MeshRenderer>();
            Debug.Assert(tileRenderer, "FarmTile requires a MeshRenderer");

            foreach (Transform edge in transform)
            {
                materials.Add(edge.gameObject.GetComponent<MeshRenderer>().material);
            }
            currentWater = 0f;
            waterDecayPerSecond *= .02f; // hopefully accounts for FixedUpdate frequency
        }

        private void FixedUpdate()
        {
            if (currentWater > 0)
            {
                currentWater -= waterDecayPerSecond;
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
                    if (!currentPlant.RegrowsFruit) // Only remove plant if it can't regrow
                    {
                        ClearPlant();
                        Till();
                    }
                } break;
            }
            daysSinceLastInteraction = 0;
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
        private void SetNoWater()
        {
            currentWater = 0f;
        }

        // TODO: Check if we need to destroy plantObj at any point
        GameObject plantObj;

        // Spawns plant prefab and transitions tile into planted state.
        private void PlantSeed()
        {
            if (currentPlant != null) return;

            plantObj = Instantiate(plantPrefab, transform.position, Quaternion.identity);
            currentPlant = plantObj.GetComponent<Plant>();
            currentPlant.SetParentTile(this);
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
            Destroy(currentPlant.gameObject);
            currentPlant = null;
            tileCondition = Condition.Grass;
            UpdateVisual();
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
