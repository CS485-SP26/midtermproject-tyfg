using UnityEngine;
using UnityEngine.SceneManagement;

namespace Farming
{
    public class FarmWinController : RewardControllerBase
    {
        public const string AllTilesRewardGivenFlag = "farm_all_tiles_reward_given";

        [Header("Win Reward")]
        [SerializeField] private int fundsReward = 25;
        [SerializeField] private float checkInterval = 0.2f;
        [SerializeField] private string congratsMessage = "All tiles watered! Funds awarded.";
        [SerializeField] private Color congratsColor = new Color(0.9f, 1f, 0.9f, 1f);

        private float checkTimer = 0f;
        private FarmTile[] farmTiles = new FarmTile[0];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallBootstrap()
        {
            SceneManager.sceneLoaded -= HandleSceneLoadedStatic;
            SceneManager.sceneLoaded += HandleSceneLoadedStatic;
            EnsureControllerForActiveScene();
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            if (fundsReward < 1)
                fundsReward = 1;

            if (checkInterval < 0.05f)
                checkInterval = 0.05f;
        }

        protected override void Start()
        {
            base.Start();
            RefreshTiles();
            EvaluateWinCondition();
        }

        protected override void Update()
        {
            base.Update();

            checkTimer -= Time.deltaTime;
            if (checkTimer > 0f)
                return;

            checkTimer = checkInterval;
            EvaluateWinCondition();
        }

        private void RefreshTiles()
        {
            farmTiles = FindObjectsByType<FarmTile>(FindObjectsSortMode.None);
        }

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

        private static void HandleSceneLoadedStatic(Scene scene, LoadSceneMode mode)
        {
            EnsureControllerForActiveScene();
        }

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
