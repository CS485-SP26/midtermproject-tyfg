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

        void Start()
        {
            tileRenderer = GetComponent<MeshRenderer>();
            Debug.Assert(tileRenderer, "FarmTile requires a MeshRenderer");

            foreach (Transform edge in transform)
            {
                materials.Add(edge.gameObject.GetComponent<MeshRenderer>().material);
            }
        }

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

        public void Till()
        {
            tileCondition = FarmTile.Condition.Tilled;
            UpdateVisual();
            tillAudio?.Play();
        }

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
        private void PlantSeed()
        {
            if (currentPlant != null)
                return;

            GameObject plantObj = Instantiate(plantPrefab, plantSpawnPoint.position, Quaternion.identity);
            currentPlant = plantObj.GetComponent<Plant>();

            tileCondition = Condition.Planted;
            UpdateVisual();
        }
        private void ClearPlant()
        {
            Destroy(currentPlant.gameObject);
            currentPlant = null;
            tileCondition = Condition.Grass;
            UpdateVisual();
        }

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
