using System.Collections.Generic;
using UnityEngine;

/*
* This class checks for the win condition of the farm scene, which is when all farmable tiles are watered. It periodically checks the 
*   state of all farm tiles and awards the player with funds if a tile is newly watered.
* Exposes:
*   - NotifyTileStatePotentiallyChanged(): A static method that can be called by farm tiles when their state changes to trigger a 
*    re-evaluation of the win condition.
* Requires:
*   - A reference to the GameManager to check and set flags for reward distribution.
*/

namespace Farming
{
    public class PerTileWaterRewardController : RewardControllerBase
    {
        // Reward granted per tile transition into Watered state.
        [SerializeField] private int fundsPerTile = 2;
        // Polling cadence for reward scans.
        [SerializeField] private float scanIntervalSeconds = 0.2f;
        // If true, already-watered tiles can count when first observed.
        [SerializeField] private bool awardAlreadyWateredOnStart = false;
        // Notification format string receiving total payout amount.
        [SerializeField] private string rewardMessageFormat = "+${0} for watering tiles";
        [SerializeField] private Color rewardColor = new Color(0.6f, 1f, 0.6f, 1f);

        private float nextScanTime = 0f;
        private FarmTile[] farmTiles = new FarmTile[0];
        private readonly Dictionary<FarmTile, FarmTile.Condition> lastSeenCondition = new Dictionary<FarmTile, FarmTile.Condition>();

        // Validates serialized values for stable runtime behavior.
        protected override void OnValidate()
        {
            base.OnValidate();

            if (fundsPerTile < 1)
                fundsPerTile = 1;

            if (scanIntervalSeconds < 0.05f)
                scanIntervalSeconds = 0.05f;
        }

        // Builds initial tile cache and baseline condition map.
        protected override void Start()
        {
            base.Start();
            RefreshTiles();
            SeedLastSeenValues();
        }

        // Performs periodic tile scan and awards newly-watered transitions.
        protected override void Update()
        {
            base.Update();

            if (Time.time < nextScanTime)
                return;

            nextScanTime = Time.time + scanIntervalSeconds;
            EvaluateTileRewards();
        }

        // Refreshes tile list from active scene.
        private void RefreshTiles()
        {
            farmTiles = FindObjectsByType<FarmTile>(FindObjectsSortMode.None);
        }

        // Captures initial condition snapshot for each tracked tile.
        private void SeedLastSeenValues()
        {
            lastSeenCondition.Clear();
            foreach (FarmTile tile in farmTiles)
            {
                if (tile == null)
                    continue;

                lastSeenCondition[tile] = tile.TileCondition;
            }
        }

        // Detects tiles entering Watered state and issues aggregated reward.
        private void EvaluateTileRewards()
        {
            if (farmTiles == null || farmTiles.Length == 0)
            {
                RefreshTiles();
                if (farmTiles.Length == 0)
                    return;
            }

            int newlyWateredCount = 0;
            foreach (FarmTile tile in farmTiles)
            {
                if (tile == null)
                    continue;

                FarmTile.Condition current = tile.TileCondition;
                if (!lastSeenCondition.TryGetValue(tile, out FarmTile.Condition previous))
                {
                    previous = awardAlreadyWateredOnStart ? FarmTile.Condition.Grass : current;
                }

                if (current == FarmTile.Condition.Watered && previous != FarmTile.Condition.Watered)
                    newlyWateredCount++;

                lastSeenCondition[tile] = current;
            }

            if (newlyWateredCount > 0)
            {
                int totalFunds = newlyWateredCount * fundsPerTile;
                string message = string.Format(rewardMessageFormat, totalFunds);
                AwardFundsAndNotify(totalFunds, message, rewardColor);
            }
        }
    }
}
