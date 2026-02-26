using System.Collections.Generic;
using UnityEngine;
using Environment;
using Core;

namespace Farming 
{
    public class FarmTile : MonoBehaviour
    {
        private const int FallbackAllTilesRewardFunds = 25;

        public enum Condition { Grass, Tilled, Watered, Planted }

        [SerializeField] private Condition tileCondition = Condition.Grass; 
        [SerializeField] private Transform plantSpawnPoint;
        [SerializeField] private GameObject plantPrefab;

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

        // Spawns plant prefab and transitions tile into planted state.
        private void PlantSeed()
        {
            if (currentPlant != null)
                return;

            GameObject plantObj = Instantiate(plantPrefab, plantSpawnPoint.position, Quaternion.identity);
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
