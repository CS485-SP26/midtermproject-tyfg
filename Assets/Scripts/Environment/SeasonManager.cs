using UnityEngine;
public enum Season
{
    Spring,
    Summer,
    Fall,
    Winter
}
namespace Environment
{
    public class SeasonManager : MonoBehaviour
    {
        public static SeasonManager Instance;

        public Season CurrentSeason = Season.Spring;

        public int daysPerSeason = 1;
        private int currentDay = 1;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // ← keeps it across scenes
            }
            else
            {
                Destroy(gameObject); // prevents duplicates
            }
        }

        public void AdvanceDay()
        {
            currentDay++;

            if(currentDay > daysPerSeason)
            {
                currentDay = 1;
                AdvanceSeason();
            }
        }

        void AdvanceSeason()
        {
            CurrentSeason++;

            if((int)CurrentSeason > 3)
                CurrentSeason = Season.Spring;

            Debug.Log("Season changed to: " + CurrentSeason);
        }
    }
}