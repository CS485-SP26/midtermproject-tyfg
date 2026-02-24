using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core
{
    public class GameManager : MonoBehaviour, IEconomyService
    {
        private static GameManager instance;
        private readonly HashSet<string> progressFlags = new HashSet<string>();
        private readonly Dictionary<EconomyResource, int> resourceBalances = new Dictionary<EconomyResource, int>();

        [Header("Starting Data")]
        [SerializeField] private int startingFunds = 0;
        [SerializeField] private int startingSeeds = 0;
        [SerializeField] private int startingSkillPoints = 0;

        public int Funds => GetResourceAmount(EconomyResource.Funds);
        public int Seeds => GetResourceAmount(EconomyResource.Seeds);
        public int SkillPoints => GetResourceAmount(EconomyResource.SkillPoints);

        public event Action<int> FundsChanged;
        public event Action<int> SeedsChanged;
        public event Action<int> SkillPointsChanged;
        public event Action<EconomyResource, int> ResourceChanged;

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
            InitializeResourceBalances();
            DontDestroyOnLoad(this.gameObject);

            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            EnsureSingleAudioListener();
            Debug.Log("GameManager initialized with Funds: " + Funds + ", Seeds: " + Seeds + ", Skill Points: " + SkillPoints);
        }

        private void OnDestroy()
        {
            if (instance == this)
                SceneManager.sceneLoaded -= HandleSceneLoaded;
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

        public int GetResourceAmount(EconomyResource resource)
        {
            return resourceBalances.TryGetValue(resource, out int value) ? value : 0;
        }

        public void AddResource(EconomyResource resource, int amount)
        {
            if (amount <= 0)
                return;

            SetResourceAmount(resource, GetResourceAmount(resource) + amount, true);
        }

        public bool TrySpendResource(EconomyResource resource, int amount)
        {
            if (amount < 0)
                return false;

            if (amount == 0)
                return true;

            int current = GetResourceAmount(resource);
            if (current < amount)
                return false;

            SetResourceAmount(resource, current - amount, true);
            return true;
        }

        public void AddFunds(int amount)
        {
            AddResource(EconomyResource.Funds, amount);
        }

        public bool TrySpendFunds(int amount)
        {
            return TrySpendResource(EconomyResource.Funds, amount);
        }

        public void AddSeeds(int amount)
        {
            AddResource(EconomyResource.Seeds, amount);
        }

        public bool TryConsumeSeeds(int amount)
        {
            return TrySpendResource(EconomyResource.Seeds, amount);
        }

        public void AddSkillPoints(int amount)
        {
            AddResource(EconomyResource.SkillPoints, amount);
        }

        public bool TrySpendSkillPoints(int amount)
        {
            return TrySpendResource(EconomyResource.SkillPoints, amount);
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
            InitializeResourceBalances();
            progressFlags.Clear();

            NotifyAllResourceChanged();
        }

        public void ResetSessionData(int funds, int seeds, int skillPoints)
        {
            startingFunds = Mathf.Max(0, funds);
            startingSeeds = Mathf.Max(0, seeds);
            startingSkillPoints = Mathf.Max(0, skillPoints);
            ResetSessionData();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureSingleAudioListener();
        }

        private void InitializeResourceBalances()
        {
            resourceBalances[EconomyResource.Funds] = Mathf.Max(0, startingFunds);
            resourceBalances[EconomyResource.Seeds] = Mathf.Max(0, startingSeeds);
            resourceBalances[EconomyResource.SkillPoints] = Mathf.Max(0, startingSkillPoints);
        }

        private void SetResourceAmount(EconomyResource resource, int amount, bool notify)
        {
            int clampedAmount = Mathf.Max(0, amount);
            if (resourceBalances.TryGetValue(resource, out int current) && current == clampedAmount)
                return;

            resourceBalances[resource] = clampedAmount;
            if (notify)
                NotifyResourceChanged(resource, clampedAmount);
        }

        private void NotifyAllResourceChanged()
        {
            NotifyResourceChanged(EconomyResource.Funds, Funds);
            NotifyResourceChanged(EconomyResource.Seeds, Seeds);
            NotifyResourceChanged(EconomyResource.SkillPoints, SkillPoints);
        }

        private void NotifyResourceChanged(EconomyResource resource, int value)
        {
            ResourceChanged?.Invoke(resource, value);

            switch (resource)
            {
                case EconomyResource.Funds:
                    FundsChanged?.Invoke(value);
                    break;

                case EconomyResource.Seeds:
                    SeedsChanged?.Invoke(value);
                    break;

                case EconomyResource.SkillPoints:
                    SkillPointsChanged?.Invoke(value);
                    break;
            }
        }

        private static void EnsureSingleAudioListener()
        {
            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            if (listeners == null || listeners.Length == 0)
                return;

            if (listeners.Length == 1)
            {
                if (!listeners[0].enabled)
                    listeners[0].enabled = true;
                return;
            }

            AudioListener keep = null;
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
                keep = mainCamera.GetComponent<AudioListener>();

            if (keep == null)
            {
                foreach (AudioListener listener in listeners)
                {
                    if (listener != null && listener.enabled && listener.gameObject.activeInHierarchy)
                    {
                        keep = listener;
                        break;
                    }
                }
            }

            if (keep == null)
                keep = listeners[0];

            foreach (AudioListener listener in listeners)
            {
                if (listener == null)
                    continue;

                listener.enabled = listener == keep;
            }
        }
    }

    // Backwards compatibility for existing scene references still targeting Core.LoadScene.
    public class LoadScene : MonoBehaviour
    {
        [Header("Starting Data")]
        [SerializeField] private int startingFunds = 0;
        [SerializeField] private int startingSeeds = 0;
        [SerializeField] private int startingSkillPoints = 0;
        [SerializeField] private bool applyStartingDataOnFirstAwake = true;

        private static bool hasAppliedStartingData;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            hasAppliedStartingData = false;
        }

        private void Awake()
        {
            if (!applyStartingDataOnFirstAwake || hasAppliedStartingData)
                return;

            GameManager manager = GameManager.Instance;
            if (manager != null)
                manager.ResetSessionData(startingFunds, startingSeeds, startingSkillPoints);

            hasAppliedStartingData = true;
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
    }
}
