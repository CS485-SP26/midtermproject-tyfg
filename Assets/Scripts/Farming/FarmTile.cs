using System.Collections.Generic;
using UnityEngine;
using Environment;
using Core;
using UnityEngine.UIElements;

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
    public class FarmTile : MonoBehaviour
    {
        private const int FallbackAllTilesRewardFunds = 25;

        public enum Condition { Grass, Tilled, Watered, Planted }

        [SerializeField] private Condition tileCondition = Condition.Grass; 
        [SerializeField] private Transform plantSpawnPoint;
        [SerializeField] private GameObject plantPrefab;

        private Vector3 plantSpawnPointPos; // Set in Start()

        // Runtime plant instance currently occupying this tile (if any).
private Plant currentPlant;
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

        private int daysSinceLastInteraction = 0;
        public FarmTile.Condition GetCondition { get { return tileCondition; } } // TODO: Consider what the set would do?

        // Caches renderer references and highlight materials.
        void Start()
        {
            tileRenderer = GetComponent<MeshRenderer>();
            Debug.Assert(tileRenderer, "FarmTile requires a MeshRenderer");
            //plantSpawnPointPos = plantSpawnPoint.position;

            plantSpawnPoint = GetComponent<Transform>();

            foreach (Transform edge in transform)
            {
                materials.Add(edge.gameObject.GetComponent<MeshRenderer>().material);
            }
        }

        // Primary interaction state machine for till/water/plant progression.
        public void Interact()
        {
            switch(tileCondition)
            {
                case FarmTile.Condition.Grass: Till(); break;
                case FarmTile.Condition.Tilled: Water(); break;
                case FarmTile.Condition.Watered: PlantSeed();break;
                case FarmTile.Condition.Planted:
                {
                    // ClearPlant();
                    // Till();
                    Water();
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
            if (tileCondition == Condition.Planted && currentPlant != null)
             {
                currentPlant.AddWater(1f);
                waterAudio?.Play();
                return;
            }

            tileCondition = Condition.Watered;
            UpdateVisual();
            waterAudio?.Play();
        }


        // TODO: Check if we need to destroy plantObj at any point
        GameObject plantObj;

        // Spawns plant prefab and transitions tile into planted state.
        private void PlantSeed()
        {
            if (currentPlant != null) return;

            //GameObject plantObj = Instantiate(plantPrefab, plantSpawnPoint.position, Quaternion.identity);
            
            // TODO: Overwritten for testing purposes: 
            //plantSpawnPointPos = Vector3.zero;

            plantObj = Instantiate(plantPrefab, plantSpawnPointPos, Quaternion.identity);
            
            currentPlant = plantObj.GetComponent<Plant>();

            tileCondition = Condition.Planted;
            UpdateVisual();
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

            if(daysSinceLastInteraction >= 2)
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
                if (tile.GetCondition != FarmTile.Condition.Watered)
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
