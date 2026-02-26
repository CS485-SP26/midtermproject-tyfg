using UnityEngine;
using UnityEngine.SceneManagement;

/*
* This class checks for the win condition of the farm scene, which is when all farmable tiles are watered. It periodically checks the state
     of all farm tiles and awards the player with funds if the win condition is met. It also ensures that the reward is only given once per 
        win condition occurrence by using a flag in the GameManager.
* Exposes:
*   - NotifyTileStatePotentiallyChanged(): A static method that can be called by farm tiles when their state changes to trigger a re-evaluation of the win condition.
* Requires:
*   - A reference to the GameManager to check and set flags for reward distribution.    
*/

namespace Farming
{
    public class FarmWinController : RewardControllerBase
    {
        // Session flag key used to avoid duplicate all-tiles reward payout.
        public const string AllTilesRewardGivenFlag = "farm_all_tiles_reward_given";

        [Header("Win Reward")]
        [SerializeField] private int fundsReward = 25;
        [SerializeField] private float checkInterval = 0.2f;
        [SerializeField] private string congratsMessage = "All tiles watered! Funds awarded.";
        [SerializeField] private Color congratsColor = new Color(0.9f, 1f, 0.9f, 1f);

        private float checkTimer = 0f;
        private FarmTile[] farmTiles = new FarmTile[0];

        // Installs a scene-load bootstrap so controller auto-exists in farm scenes.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallBootstrap()
        {
            SceneManager.sceneLoaded -= HandleSceneLoadedStatic;
            SceneManager.sceneLoaded += HandleSceneLoadedStatic;
            EnsureControllerForActiveScene();
        }

        // Validates serialized reward/check interval values.
        protected override void OnValidate()
        {
            base.OnValidate();

            if (fundsReward < 1)
                fundsReward = 1;

            if (checkInterval < 0.05f)
                checkInterval = 0.05f;
        }

        // Initializes tile cache and evaluates current win state.
        protected override void Start()
        {
            base.Start();
            RefreshTiles();
            EvaluateWinCondition();
        }

        // Periodically reevaluates win condition at configured interval.
        protected override void Update()
        {
            base.Update();

            checkTimer -= Time.deltaTime;
            if (checkTimer > 0f)
                return;

            checkTimer = checkInterval;
            EvaluateWinCondition();
        }

        // Refreshes tile cache from current scene objects.
        private void RefreshTiles()
        {
            farmTiles = FindObjectsByType<FarmTile>(FindObjectsSortMode.None);
        }

        // Awards win reward once when all farmable tiles are watered.
        private void EvaluateWinCondition()
        {
            if (farmTiles == null || farmTiles.Length == 0)
                RefreshTiles();

            Core.GameManager gameManager = Core.GameManager.Instance;
            bool allWatered = AreAllTilesWatered();
            if (allWatered)
            {
                if (!gameManager.IsFlagSet(AllTilesRewardGivenFlag))
                {
                    AwardFundsAndNotify(fundsReward, congratsMessage, congratsColor);
                    gameManager.SetFlag(AllTilesRewardGivenFlag, true);
                }
            }
            else
            {
                gameManager.SetFlag(AllTilesRewardGivenFlag, false);
            }
        }

        // Returns true when every non-purchase tile is in Watered state.
        private bool AreAllTilesWatered()
        {
            if (farmTiles == null || farmTiles.Length == 0)
                return false;

            bool foundAnyFarmableTile = false;
            foreach (FarmTile tile in farmTiles)
            {
                if (tile == null)
                    continue;

                if (tile.GetComponent<SeedPurchaseTile>() != null)
                    continue;

                foundAnyFarmableTile = true;
                if (tile.GetCondition != FarmTile.Condition.Watered)
                    return false;
            }

            return foundAnyFarmableTile;
        }

        // Static scene-load callback for bootstrapping.
        private static void HandleSceneLoadedStatic(Scene scene, LoadSceneMode mode)
        {
            EnsureControllerForActiveScene();
        }

        // External hook used by tiles to force immediate win-state reevaluation.
        public static void NotifyTileStatePotentiallyChanged()
        {
            EnsureControllerForActiveScene();

            FarmWinController[] controllers = FindObjectsByType<FarmWinController>(FindObjectsSortMode.None);
            foreach (FarmWinController controller in controllers)
            {
                if (controller == null || !controller.isActiveAndEnabled)
                    continue;

                controller.EvaluateWinCondition();
            }
        }

        // Creates controller object if farm tiles exist but controller is missing.
        private static void EnsureControllerForActiveScene()
        {
            if (FindObjectsByType<FarmWinController>(FindObjectsSortMode.None).Length > 0)
                return;

            if (FindObjectsByType<FarmTile>(FindObjectsSortMode.None).Length == 0)
                return;

            GameObject go = new GameObject("FarmWinController");
            go.AddComponent<FarmWinController>();
        }
    }
}
