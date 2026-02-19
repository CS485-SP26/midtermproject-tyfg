using System.Collections.Generic;
using UnityEngine;

namespace Farming
{
    public class PerTileWaterRewardController : RewardControllerBase
    {
        [SerializeField] private int fundsPerTile = 2;
        [SerializeField] private float scanIntervalSeconds = 0.2f;
        [SerializeField] private bool awardAlreadyWateredOnStart = false;
        [SerializeField] private string rewardMessageFormat = "+${0} for watering tiles";
        [SerializeField] private Color rewardColor = new Color(0.6f, 1f, 0.6f, 1f);

        private float nextScanTime = 0f;
        private FarmTile[] farmTiles = new FarmTile[0];
        private readonly Dictionary<FarmTile, FarmTile.Condition> lastSeenCondition = new Dictionary<FarmTile, FarmTile.Condition>();

        protected override void OnValidate()
        {
            base.OnValidate();

            if (fundsPerTile < 1)
                fundsPerTile = 1;

            if (scanIntervalSeconds < 0.05f)
                scanIntervalSeconds = 0.05f;
        }

        protected override void Start()
        {
            base.Start();
            RefreshTiles();
            SeedLastSeenValues();
        }

        protected override void Update()
        {
            base.Update();

            if (Time.time < nextScanTime)
                return;

            nextScanTime = Time.time + scanIntervalSeconds;
            EvaluateTileRewards();
        }

        private void RefreshTiles()
        {
            farmTiles = FindObjectsByType<FarmTile>(FindObjectsSortMode.None);
        }

        private void SeedLastSeenValues()
        {
            lastSeenCondition.Clear();
            foreach (FarmTile tile in farmTiles)
            {
                if (tile == null)
                    continue;

                lastSeenCondition[tile] = tile.GetCondition;
            }
        }

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

                FarmTile.Condition current = tile.GetCondition;
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
