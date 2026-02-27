using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/*
* The GameManager is a singleton that manages game-wide state, including:
*   - Player progress flags (e.g. has the player completed a certain quest or unlocked a feature)
*   - Economy resources (funds, seeds, skill points) with methods to add and spend resources, and events for when resources change
*   - Scene loading with methods to load scenes by name and an event handler for when scenes are loaded
*   - Ensuring only one AudioListener is active in the scene to prevent audio issues
* Exposes:
*   - Funds, Seeds, SkillPoints properties to get current resource amounts
*   - Events: FundsChanged, SeedsChanged, SkillPointsChanged, ResourceChanged
*   - Methods to add and spend resources, check and set progress flags, and reset session data
* Requires:
*   - Scenes must have a GameManager in them or rely on the singleton to persist across scenes.
*   - Other scripts can subscribe to resource change events to update UI or trigger other effects
*/

namespace Core
{
    public class GameManager : MonoBehaviour, IEconomyService
    {
        // Singleton instance used for global access.
        private static GameManager instance;
        // Session flags for one-run progression checks (tutorial done, intro seen, etc.).
        private readonly HashSet<string> progressFlags = new HashSet<string>();
        // Current balances for each tracked economy resource.
        private readonly Dictionary<EconomyResource, int> resourceBalances = new Dictionary<EconomyResource, int>();

        [Header("Starting Data")]
        // Initial values used when the session starts or resets.
        [SerializeField] private int startingFunds = 0;
        [SerializeField] private int startingSeeds = 15;
        [SerializeField] private int startingSkillPoints = 0;

        // Convenience read-only properties for common resource lookups.
        public int Funds => GetResourceAmount(EconomyResource.Funds);
        public int Seeds => GetResourceAmount(EconomyResource.Seeds);
        public int SkillPoints => GetResourceAmount(EconomyResource.SkillPoints);

        // Resource-specific events for scripts that only care about one value.
        public event Action<int> FundsChanged;
        public event Action<int> SeedsChanged;
        public event Action<int> SkillPointsChanged;
        // Other scripts can subscribe to this resource-change event to update UI or trigger other effects.
        public event Action<EconomyResource, int> ResourceChanged;

        // Returns the global GameManager instance, creating one if none exists yet.
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

        // Enforces singleton behavior and initializes runtime systems on startup.
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

        // Unsubscribes scene callbacks when this singleton is destroyed.
        private void OnDestroy()
        {
            if (instance == this)
                SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        // Legacy wrapper that forwards to the correctly cased method name.
        public void LoadScenebyName(string sceneName)
        {
            LoadSceneByName(sceneName);
        }

        // Loads a scene by name after validating input.
        public void LoadSceneByName(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("LoadSceneByName was called with an empty scene name.");
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

        // Returns current amount for a specific economy resource (or 0 if missing).
        public int GetResourceAmount(EconomyResource resource)
        {
            return resourceBalances.TryGetValue(resource, out int value) ? value : 0;
        }

        // Adds resource amount if positive, then notifies listeners.
        public void AddResource(EconomyResource resource, int amount)
        {
            if (amount <= 0)
                return;

            SetResourceAmount(resource, GetResourceAmount(resource) + amount, true);
        }

        // Tries to spend resource amount safely; returns false if insufficient balance.
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

        // Convenience wrapper for adding Funds.
        public void AddFunds(int amount)
        {
            AddResource(EconomyResource.Funds, amount);
        }

        // Convenience wrapper for spending Funds.
        public bool TrySpendFunds(int amount)
        {
            return TrySpendResource(EconomyResource.Funds, amount);
        }

        // Convenience wrapper for adding Seeds.
        public void AddSeeds(int amount)
        {
            AddResource(EconomyResource.Seeds, amount);
        }

        // Convenience wrapper for consuming Seeds.
        public bool TryConsumeSeeds(int amount)
        {
            return TrySpendResource(EconomyResource.Seeds, amount);
        }

        // Convenience wrapper for adding SkillPoints.
        public void AddSkillPoints(int amount)
        {
            AddResource(EconomyResource.SkillPoints, amount);
        }

        // Convenience wrapper for spending SkillPoints.
        public bool TrySpendSkillPoints(int amount)
        {
            return TrySpendResource(EconomyResource.SkillPoints, amount);
        }

        // Returns true if a named progress flag exists.
        public bool IsFlagSet(string flag)
        {
            return !string.IsNullOrWhiteSpace(flag) && progressFlags.Contains(flag);
        }

        // Sets or clears a named progress flag.
        public void SetFlag(string flag, bool value = true)
        {
            if (string.IsNullOrWhiteSpace(flag))
                return;

            if (value)
                progressFlags.Add(flag);
            else
                progressFlags.Remove(flag);
        }

        // Resets resources to configured starting data and clears session flags.
        public void ResetSessionData()
        {
            InitializeResourceBalances();
            progressFlags.Clear();

            NotifyAllResourceChanged();
        }

        // Updates starting values, then performs a full session reset.
        public void ResetSessionData(int funds, int seeds, int skillPoints)
        {
            startingFunds = Mathf.Max(0, funds);
            startingSeeds = Mathf.Max(0, seeds);
            startingSkillPoints = Mathf.Max(0, skillPoints);
            ResetSessionData();
        }

        // Scene callback used to re-validate scene-specific singleton constraints.
        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureSingleAudioListener();
        }

        // Initializes all tracked resources using sanitized starting values.
        private void InitializeResourceBalances()
        {
            resourceBalances[EconomyResource.Funds] = Mathf.Max(0, startingFunds);
            resourceBalances[EconomyResource.Seeds] = Mathf.Max(0, startingSeeds);
            resourceBalances[EconomyResource.SkillPoints] = Mathf.Max(0, startingSkillPoints);
        }

        // Centralized setter that clamps value and optionally emits change events.
        private void SetResourceAmount(EconomyResource resource, int amount, bool notify)
        {
            int clampedAmount = Mathf.Max(0, amount);
            if (resourceBalances.TryGetValue(resource, out int current) && current == clampedAmount)
                return;

            resourceBalances[resource] = clampedAmount;
            if (notify)
                NotifyResourceChanged(resource, clampedAmount);
        }

        // Fires change notifications for every resource, useful after resets.
        private void NotifyAllResourceChanged()
        {
            NotifyResourceChanged(EconomyResource.Funds, Funds);
            NotifyResourceChanged(EconomyResource.Seeds, Seeds);
            NotifyResourceChanged(EconomyResource.SkillPoints, SkillPoints);
        }

        // Emits generic + resource-specific events for a changed balance.
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

        // Ensures only one active AudioListener exists to prevent Unity warnings/audio issues.
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
        // Optional one-time session data applied from older scene bootstrap usage.
        [SerializeField] private int startingFunds = 0;
        [SerializeField] private int startingSeeds = 0;
        [SerializeField] private int startingSkillPoints = 0;
        [SerializeField] private bool applyStartingDataOnFirstAwake = true;

        // Tracks whether legacy starting data was already applied this play session.
        private static bool hasAppliedStartingData;

        // Resets static data when play mode/runtime subsystem starts.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            hasAppliedStartingData = false;
        }

        // Applies configured starting data once, then marks it as consumed.
        private void Awake()
        {
            if (!applyStartingDataOnFirstAwake || hasAppliedStartingData)
                return;

            GameManager manager = GameManager.Instance;
            if (manager != null)
                manager.ResetSessionData(startingFunds, startingSeeds, startingSkillPoints);

            hasAppliedStartingData = true;
        }

        // Legacy wrapper that forwards to the correctly cased method name.
        public void LoadScenebyName(string sceneName)
        {
            LoadSceneByName(sceneName);
        }

        // Loads a scene by name after validating input.
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
