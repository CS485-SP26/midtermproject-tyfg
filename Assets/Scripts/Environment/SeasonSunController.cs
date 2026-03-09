using UnityEngine;

namespace Environment
{
    public class SeasonSunController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Light sunLight;

        [Header("Season Settings")]
        [SerializeField] private Color springColor = new Color(1f, 0.95f, 0.8f);
        [SerializeField] private Color summerColor = Color.white;
        [SerializeField] private Color fallColor = new Color(1f, 0.75f, 0.5f);
        [SerializeField] private Color winterColor = new Color(0.75f, 0.85f, 1f);

        [SerializeField] private float springIntensity = 1.1f;
        [SerializeField] private float summerIntensity = 1.3f;
        [SerializeField] private float fallIntensity = 0.9f;
        [SerializeField] private float winterIntensity = 0.6f;

        private Season currentSeason;

        private void Start()
        {
            if (sunLight == null)
                sunLight = GetComponent<Light>();

            UpdateSeasonLighting();
        }

        private void Update()
        {
            if (SeasonManager.Instance == null)
                return;

            if (currentSeason != SeasonManager.Instance.CurrentSeason)
            {
                currentSeason = SeasonManager.Instance.CurrentSeason;
                UpdateSeasonLighting();
            }
        }

        private void UpdateSeasonLighting()
        {
            switch (currentSeason)
            {
                case Season.Spring:
                    sunLight.color = springColor;
                    sunLight.intensity = springIntensity;
                    break;

                case Season.Summer:
                    sunLight.color = summerColor;
                    sunLight.intensity = summerIntensity;
                    break;

                case Season.Fall:
                    sunLight.color = fallColor;
                    sunLight.intensity = fallIntensity;
                    break;

                case Season.Winter:
                    sunLight.color = winterColor;
                    sunLight.intensity = winterIntensity;
                    break;
            }
        }
    }
}