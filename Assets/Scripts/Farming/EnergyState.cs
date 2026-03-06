using System;
using Core;
using UnityEngine;

namespace Farming
{
    // Runtime energy source of truth used by gameplay and HUD presenters.
    public class EnergyState : MonoBehaviour, IResourceState
    {
        private static EnergyState instance;

        private float max = 100f;
        private float regenPerSecond = 8f;
        private float current;
        private bool initialized;
        private bool actorPresent = true;

        public static EnergyState Instance
        {
            get
            {
                EnsureInstance();
                return instance;
            }
        }

        public float Current => current;
        public float Max => max;
        public bool IsInitialized => initialized;
        public event Action<float, float> ValueChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            EnsureInstance();
        }

        private static void EnsureInstance()
        {
            if (instance != null)
                return;

            GameObject go = new GameObject(nameof(EnergyState));
            instance = go.AddComponent<EnergyState>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (!initialized)
                return;

            if (!actorPresent && current < max)
                SetValue(current + (regenPerSecond * Time.deltaTime));
        }

        public void Configure(float maxValue, float regenValue)
        {
            max = Mathf.Max(1f, maxValue);
            regenPerSecond = Mathf.Max(0f, regenValue);

            if (initialized)
                SetValue(current);
        }

        public void InitializeIfNeeded(float startingValue)
        {
            if (initialized)
                return;

            initialized = true;
            current = Mathf.Clamp(startingValue, 0f, max);
            ValueChanged?.Invoke(current, max);
        }

        public void SetActorPresent(bool present)
        {
            actorPresent = present;
        }

        public void SetValue(float value)
        {
            initialized = true;
            float clamped = Mathf.Clamp(value, 0f, max);
            if (Mathf.Approximately(clamped, current))
                return;

            current = clamped;
            ValueChanged?.Invoke(current, max);
        }
    }
}
