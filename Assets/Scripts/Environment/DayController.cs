using UnityEngine;
using TMPro; // Important for TextMeshPro
using UnityEngine.Events;
using Farming;

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
        [Header("Object References")]
        // Directional light used as sun for day/night visuals.
        [SerializeField] private Light sunLight;
        // UI label showing current day.
        [SerializeField] private TMP_Text dayLabel;
        
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

        // Initializes day label text on startup.
        private void Start()
        {
            if (dayLabel != null)
                dayLabel.SetText("Days: {0}", currentDay);
        }

        // Advances to next day, resets progress, updates label, and notifies listeners.
        public void AdvanceDay()
        {
            Debug.Assert(sunLight, "DayController requires a 'Sun'");
            if (dayLabel == null) Debug.Log("DayController does not have a label to update");

            dayProgressSeconds = 0f; // Reset to start a new day.
            currentDay++;
            
            if (dayLabel)
            {
                // Avoid string-concat GC churn by using SetText formatting.
                dayLabel.SetText("Days: {0}", currentDay);                
            }

            dayPassedEvent.Invoke(); // Make announcement to all listeners.
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
            dayProgressSeconds += Time.deltaTime;

            if (dayProgressSeconds >= dayLengthSeconds)
            {
                AdvanceDay();
            }

            UpdateVisuals();
        }
    }
}
