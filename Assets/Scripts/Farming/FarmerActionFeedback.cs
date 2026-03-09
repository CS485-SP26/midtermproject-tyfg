using Core;
using UnityEngine;

namespace Farming
{
    [System.Serializable]
    public sealed class FarmerActionFeedbackSettings
    {
        [SerializeField] private Canvas notificationCanvas;
        [SerializeField] private Vector2 feedbackAnchor = new Vector2(0.5f, 0.28f);
        [SerializeField] private Vector2 feedbackSize = new Vector2(420f, 48f);
        [SerializeField] private int feedbackFontSize = 20;
        [SerializeField] private float feedbackDurationSeconds = 0.8f;
        [SerializeField] private float feedbackRisePixels = 28f;
        [SerializeField] private float feedbackCooldownSeconds = 0.6f;

        public Canvas NotificationCanvas => notificationCanvas;
        public Vector2 FeedbackAnchor => feedbackAnchor;
        public Vector2 FeedbackSize => feedbackSize;
        public int FeedbackFontSize => feedbackFontSize;
        public float FeedbackDurationSeconds => feedbackDurationSeconds;
        public float FeedbackRisePixels => feedbackRisePixels;
        public float FeedbackCooldownSeconds => feedbackCooldownSeconds;


        // Migrate legacy settings from the old system. This allows us to reuse existing settings without breaking them.
        public void MigrateLegacy(
            Canvas legacyCanvas,
            Vector2 legacyAnchor,
            Vector2 legacySize,
            int legacyFontSize,
            float legacyDurationSeconds,
            float legacyRisePixels,
            float legacyCooldownSeconds)
        {
            if (notificationCanvas == null)
                notificationCanvas = legacyCanvas;

            feedbackAnchor = legacyAnchor;
            feedbackSize = legacySize;
            feedbackFontSize = legacyFontSize;
            feedbackDurationSeconds = legacyDurationSeconds;
            feedbackRisePixels = legacyRisePixels;
            feedbackCooldownSeconds = legacyCooldownSeconds;
            Clamp();
        }

        // Ensure all settings are within reasonable bounds to prevent issues with the feedback display.
        public void Clamp()
        {
            feedbackFontSize = Mathf.Max(10, feedbackFontSize);
            feedbackDurationSeconds = Mathf.Max(0.1f, feedbackDurationSeconds);
            feedbackRisePixels = Mathf.Max(0f, feedbackRisePixels);
            feedbackCooldownSeconds = Mathf.Max(0.05f, feedbackCooldownSeconds);
        }
    }

    // Displays temporary blocked-action messages with a cooldown.
    // A public sealed class is used here to allow other systems to create and manage their own feedback instances if needed, 
    // while keeping the implementation details hidden. This promotes encapsulation and flexibility in how feedback is used 
    // across the farming system. 
    public sealed class FarmerActionFeedback
    {
        // readonly means the settings reference cannot be changed after construction, ensuring consistent behavior. 
        // The cooldown mechanism prevents spamming feedback messages, improving user experience.
        private readonly FarmerActionFeedbackSettings settings;
        private float nextFeedbackTime;

        public FarmerActionFeedback(FarmerActionFeedbackSettings settingsRef)
        {
            settings = settingsRef;
        }

        // Attempts to show a feedback message if the cooldown has elapsed. If successful, it schedules the next allowed feedback time.
        public void TryShow(string message)
        {
            if (settings == null || string.IsNullOrWhiteSpace(message) || Time.time < nextFeedbackTime)
                return;

            IActionFeedbackService feedbackService = ActionFeedbackService.Instance;
            if (feedbackService == null)
                return;

            nextFeedbackTime = Time.time + settings.FeedbackCooldownSeconds;
            feedbackService.ShowFeedback(
                message,
                false,
                settings.NotificationCanvas,
                settings.FeedbackAnchor,
                settings.FeedbackSize,
                settings.FeedbackFontSize,
                settings.FeedbackDurationSeconds,
                settings.FeedbackRisePixels,
                Color.white,
                "FarmerFeedback");
        }
    }
}
