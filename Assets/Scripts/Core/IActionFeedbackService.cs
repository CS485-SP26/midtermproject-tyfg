using UnityEngine;

namespace Core
{
    // Shared UI feedback API for short action/result popup messages.
    public interface IActionFeedbackService
    {
        void ShowFeedback(
            string message,
            bool richText,
            Canvas preferredCanvas,
            Vector2 anchor,
            Vector2 size,
            int fontSize,
            float durationSeconds,
            float risePixels,
            Color color,
            string objectName);
    }
}
