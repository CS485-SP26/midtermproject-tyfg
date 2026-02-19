using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core
{
    public class GameManager : MonoBehaviour
    {
        private static GameManager instance;
        private readonly HashSet<string> progressFlags = new HashSet<string>();

        [Header("Starting Data")]
        [SerializeField] private int startingFunds = 0;
        [SerializeField] private int startingSeeds = 0;

        public int Funds { get; private set; }
        public int Seeds { get; private set; }

        public event Action<int> FundsChanged;
        public event Action<int> SeedsChanged;

        public static GameManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("GameManager");
                    instance = go.AddComponent<GameManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this.gameObject);
                return;
            }

            instance = this;
            Funds = startingFunds;
            Seeds = startingSeeds;
            DontDestroyOnLoad(this.gameObject);
        }

        public void LoadScenebyName(string sceneName)
        {
            LoadSceneByName(sceneName);
        }

        public void LoadSceneByName(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("LoadSceneByName was called with an empty scene name.");
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

        public void AddFunds(int amount)
        {
            if (amount <= 0)
                return;

            Funds += amount;
            FundsChanged?.Invoke(Funds);
        }

        public bool TrySpendFunds(int amount)
        {
            if (amount < 0 || Funds < amount)
                return false;

            Funds -= amount;
            FundsChanged?.Invoke(Funds);
            return true;
        }

        public void AddSeeds(int amount)
        {
            if (amount <= 0)
                return;

            Seeds += amount;
            SeedsChanged?.Invoke(Seeds);
        }

        public bool TryConsumeSeeds(int amount)
        {
            if (amount < 0 || Seeds < amount)
                return false;

            Seeds -= amount;
            SeedsChanged?.Invoke(Seeds);
            return true;
        }

        public bool IsFlagSet(string flag)
        {
            return !string.IsNullOrWhiteSpace(flag) && progressFlags.Contains(flag);
        }

        public void SetFlag(string flag, bool value = true)
        {
            if (string.IsNullOrWhiteSpace(flag))
                return;

            if (value)
                progressFlags.Add(flag);
            else
                progressFlags.Remove(flag);
        }

        public void ResetSessionData()
        {
            Funds = startingFunds;
            Seeds = startingSeeds;
            progressFlags.Clear();

            FundsChanged?.Invoke(Funds);
            SeedsChanged?.Invoke(Seeds);
        }
    }

    // Backwards compatibility for existing scene references still targeting Core.LoadScene.
    public class LoadScene : GameManager { }
}
