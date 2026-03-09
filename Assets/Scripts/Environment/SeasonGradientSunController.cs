using UnityEngine;

namespace Environment
{
    public class SeasonGradientSunController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Light sunLight;
        [SerializeField] private DayController dayController;

        [Header("Season Gradients")]
        [SerializeField] private Gradient springSunGradient;
        [SerializeField] private Gradient summerSunGradient;
        [SerializeField] private Gradient fallSunGradient;
        [SerializeField] private Gradient winterSunGradient;

        [Header("Season Intensities")]
        [SerializeField] private float springIntensity = 1.0f;
        [SerializeField] private float summerIntensity = 1.3f;
        [SerializeField] private float fallIntensity = 0.9f;
        [SerializeField] private float winterIntensity = 0.6f;

        void Update()
        {
            if (SeasonManager.Instance == null || dayController == null)
                return;

            float dayPercent = dayController.DayProgressPercent;

            Gradient activeGradient = GetSeasonGradient();

            sunLight.color = activeGradient.Evaluate(dayPercent);
            sunLight.intensity = GetSeasonIntensity();
        }

        private Gradient GetSeasonGradient()
        {
            switch (SeasonManager.Instance.CurrentSeason)
            {
                case Season.Spring:
                    return springSunGradient;

                case Season.Summer:
                    return summerSunGradient;

                case Season.Fall:
                    return fallSunGradient;

                case Season.Winter:
                    return winterSunGradient;
            }

            return springSunGradient;
        }

        private float GetSeasonIntensity()
        {
            switch (SeasonManager.Instance.CurrentSeason)
            {
                case Season.Spring:
                    return springIntensity;

                case Season.Summer:
                    return summerIntensity;

                case Season.Fall:
                    return fallIntensity;

                case Season.Winter:
                    return winterIntensity;
            }

            return 1f;
        }
    }
}