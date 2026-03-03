using UnityEngine;
using TMPro; // Important for TextMeshPro
using UnityEngine.Events;

/*
* This script manages the day-night cycle in the game. It tracks the passage of time, updates the sun's position, and triggers events at the end of each day.
* It exposes properties for the current day and the percentage of the day that has passed, allowing other scripts to react to the passage of time. It also includes a UnityEvent that is invoked at the end of each day, which can be used to trigger actions like crop growth or NPC behavior changes.
* Exposes:
*   - CurrentDay (int): The current day number, starting from 1.
*   - DayProgressPercent (float): A value between 0 and 1 representing the percentage of the current day that has passed.
*   - dayPassedEvent (UnityEvent): An event that is invoked at the end of each day, allowing other scripts to subscribe and react to the day passing.
* Requires:
*   - A Light component assigned to sunLight to represent the sun in the scene.
*   - A TextMeshPro component assigned to dayLabel to display the current day number.
*/

namespace Environment 
{
    public class DayController : MonoBehaviour
    {
        // Shared runtime day-length for off-scene simulation catch-up.
        public static float RuntimeDayLengthSeconds { get; private set; } = 60f;
        private static int runtimeCurrentDay = 1;
        private static float runtimeDayProgressSeconds = 0f;
        private static float runtimeLastRealtimeSeconds = -1f;

        [Header("Object References")]
        // Directional light used as sun for day/night visuals.
        [SerializeField] private Light sunLight;
        // UI label showing current day.
        [SerializeField] private TMP_Text dayLabel;
        [SerializeField] private string dayLabelObjectName = "DayLabel";
        
        [Header("Time Constraints")]
        // Length of a full day cycle in real seconds.
        [SerializeField] private float dayLengthSeconds = 60f;
        // Elapsed seconds in current day (inspector-visible for debugging).
        [SerializeField] private float dayProgressSeconds = 0f;
        // Current in-game day number.
        [SerializeField] private int currentDay = 1;

        // Normalized day progress [0..1].
        public float DayProgressPercent => Mathf.Clamp01(dayProgressSeconds / dayLengthSeconds);
        // Public day getter for other systems.
        public int CurrentDay { get { return currentDay; } } 

        // Invoked each time a day completes and rolls over.
        public UnityEvent dayPassedEvent = new UnityEvent();

        // Resets static runtime day state when play-mode/runtime subsystem resets.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            RuntimeDayLengthSeconds = 60f;
            runtimeCurrentDay = 1;
            runtimeDayProgressSeconds = 0f;
            runtimeLastRealtimeSeconds = -1f;
        }

        // Initializes day label text on startup.
        private void Start()
        {
            RuntimeDayLengthSeconds = Mathf.Max(1f, dayLengthSeconds);
            SyncFromRuntimeWithCatchUp();
            ResolveDayLabelIfNeeded();
            UpdateDayLabel();
        }

        // Saves current runtime day snapshot when this controller is disabled.
        private void OnDisable()
        {
            SyncRuntimeFromLocal();
        }

        // Saves current runtime day snapshot when this controller is destroyed.
        private void OnDestroy()
        {
            SyncRuntimeFromLocal();
        }

        // Advances to next day, resets progress, updates label, and notifies listeners.
        public void AdvanceDay()
        {
            Debug.Assert(sunLight, "DayController requires a 'Sun'");
            ResolveDayLabelIfNeeded();
            if (dayLabel == null) Debug.Log("DayController does not have a label to update");

            dayProgressSeconds = 0f; // Reset to start a new day.
            currentDay++;
            
            UpdateDayLabel();

            dayPassedEvent.Invoke(); // Make announcement to all listeners.
            SyncRuntimeFromLocal();
        }

        // Applies visual changes (sun rotation) based on current day progress.
        public void UpdateVisuals()
        {
            // 0 = sunrise, 180 = sunset, 360 = next sunrise.
            float sunRotationX = Mathf.Lerp(0f, 360f, DayProgressPercent);

            // Apply rotation to sun light.
            sunLight.transform.rotation = Quaternion.Euler(sunRotationX, 0f, 0f);

            // Optional extensions:
            // sunLight.intensity = 
            // RenderSettings.fogColor = 
            // RenderSettings.skybox.SetFloat(...)
        }

        // Advances day timer and updates visuals every frame.
        void Update()
        {
            RuntimeDayLengthSeconds = Mathf.Max(1f, dayLengthSeconds);
            dayProgressSeconds += Time.deltaTime;
            ResolveDayLabelIfNeeded();
            UpdateDayLabel();

            while (dayProgressSeconds >= dayLengthSeconds)
            {
                AdvanceDay();
            }

            UpdateVisuals();
            SyncRuntimeFromLocal();
        }

        // Applies elapsed real-time catch-up to day counter/progress.
        private void SyncFromRuntimeWithCatchUp()
        {
            float effectiveDayLength = Mathf.Max(1f, dayLengthSeconds);
            RuntimeDayLengthSeconds = effectiveDayLength;

            float now = Time.realtimeSinceStartup;
            if (runtimeLastRealtimeSeconds < 0f)
            {
                runtimeCurrentDay = Mathf.Max(1, currentDay);
                runtimeDayProgressSeconds = Mathf.Clamp(dayProgressSeconds, 0f, effectiveDayLength);
            }
            else
            {
                float elapsed = Mathf.Max(0f, now - runtimeLastRealtimeSeconds);
                float totalProgress = runtimeDayProgressSeconds + elapsed;
                int elapsedDays = Mathf.FloorToInt(totalProgress / effectiveDayLength);
                runtimeCurrentDay = Mathf.Max(1, runtimeCurrentDay + elapsedDays);
                runtimeDayProgressSeconds = totalProgress - (elapsedDays * effectiveDayLength);
            }

            runtimeLastRealtimeSeconds = now;
            currentDay = runtimeCurrentDay;
            dayProgressSeconds = runtimeDayProgressSeconds;
        }

        // Writes local day counter/progress to shared runtime snapshot.
        private void SyncRuntimeFromLocal()
        {
            RuntimeDayLengthSeconds = Mathf.Max(1f, dayLengthSeconds);
            runtimeCurrentDay = Mathf.Max(1, currentDay);
            runtimeDayProgressSeconds = Mathf.Clamp(dayProgressSeconds, 0f, RuntimeDayLengthSeconds);
            runtimeLastRealtimeSeconds = Time.realtimeSinceStartup;
        }

        // Updates the day counter label text if a label is available.
        private void UpdateDayLabel()
        {
            if (dayLabel != null)
                dayLabel.SetText("Days: {0}", currentDay);
        }

        // Attempts to resolve a valid day label when scene references were moved/replaced.
        private void ResolveDayLabelIfNeeded()
        {
            if (dayLabel != null)
                return;

            TMP_Text[] labels = FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
            foreach (TMP_Text label in labels)
            {
                if (label == null)
                    continue;

                if (!string.IsNullOrWhiteSpace(dayLabelObjectName) && label.name == dayLabelObjectName)
                {
                    dayLabel = label;
                    return;
                }
            }
        }
    }
}
