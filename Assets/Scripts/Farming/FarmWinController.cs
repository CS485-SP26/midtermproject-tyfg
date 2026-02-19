using Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Farming
{
    public class FarmWinController : MonoBehaviour
    {
        [Header("Win Reward")]
        [SerializeField] private int fundsReward = 25;
        [SerializeField] private float checkInterval = 0.2f;

        [Header("Congrats UI (Optional)")]
        [SerializeField] private TMP_Text congratsText;
        [SerializeField] private string congratsMessage = "All tiles watered! Funds awarded.";

        private float checkTimer = 0f;
        private bool rewardGivenForCurrentWateredState = false;
        private FarmTile[] farmTiles = new FarmTile[0];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallBootstrap()
        {
            if (FindObjectsByType<FarmWinBootstrap>(FindObjectsSortMode.None).Length > 0)
                return;

            GameObject go = new GameObject("FarmWinBootstrap");
            DontDestroyOnLoad(go);
            go.AddComponent<FarmWinBootstrap>();
        }

        private void OnValidate()
        {
            if (fundsReward < 1)
                fundsReward = 1;

            if (checkInterval < 0.05f)
                checkInterval = 0.05f;
        }

        private void Start()
        {
            RefreshTiles();
            SetCongratsVisible(false);
        }

        private void Update()
        {
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

            bool allWatered = AreAllTilesWatered();
            if (allWatered)
            {
                SetCongratsVisible(true);
                if (!rewardGivenForCurrentWateredState)
                {
                    GameManager.Instance.AddFunds(fundsReward);
                    rewardGivenForCurrentWateredState = true;
                }
            }
            else
            {
                SetCongratsVisible(false);
                rewardGivenForCurrentWateredState = false;
            }
        }

        private bool AreAllTilesWatered()
        {
            if (farmTiles == null || farmTiles.Length == 0)
                return false;

            bool foundAnyTile = false;
            foreach (FarmTile tile in farmTiles)
            {
                if (tile == null)
                    continue;

                foundAnyTile = true;
                if (tile.GetCondition != FarmTile.Condition.Watered)
                    return false;
            }

            return foundAnyTile;
        }

        private void SetCongratsVisible(bool visible)
        {
            if (congratsText == null)
                return;

            congratsText.gameObject.SetActive(visible);
            if (visible)
                congratsText.text = congratsMessage;
        }

        private class FarmWinBootstrap : MonoBehaviour
        {
            private void OnEnable()
            {
                SceneManager.sceneLoaded += HandleSceneLoaded;
            }

            private void OnDisable()
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
            }

            private void Start()
            {
                EnsureControllerForActiveScene();
            }

            private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
            {
                EnsureControllerForActiveScene();
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
}
