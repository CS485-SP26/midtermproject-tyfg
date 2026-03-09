using UnityEngine;
using System.Collections;
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
        public AudioSource musicSource;
        public AudioClip springMusic;
        public AudioClip summerMusic;
        public AudioClip fallMusic;
        public float fadeDuration = 10f; //Raise if want music to change slower
        private Coroutine fadeCoroutine; //Coroutine is just a function that runs every once in a while
        public AudioClip winterMusic;

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
        void Start()
        {
            PlaySeasonMusic();
        }
        private void OnDayPassed(int newDay)
        {
                    currentDay++;

            if (currentDay >= daysPerSeason)
            {
                currentDay = 1;
                AdvanceSeason();
            }
        }
      private void OnEnable()
        {
            DayController.DayAdvanced += OnDayPassed;
        }

        private void OnDisable()
        {
            DayController.DayAdvanced -= OnDayPassed;
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
        void PlaySeasonMusic()
        {
            if (musicSource == null)
            {
                Debug.Log("Music Source is null");
                return;
            }
            AudioClip newClip = null;
            switch (CurrentSeason)
            {
                case Season.Spring:
                    newClip = springMusic;
                    break;
                case Season.Summer:
                    newClip = summerMusic;
                    break;
                case Season.Fall:
                    newClip = fallMusic;
                    break;
                case Season.Winter:
                    newClip = winterMusic;
                    break;
            }
            if (musicSource.clip == newClip &&musicSource.isPlaying)
            {
                return;
            }
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeToNewClip(newClip));
        }
        
        IEnumerator FadeToNewClip(AudioClip newClip)
        {
            float startVolume = musicSource.volume;
            while (musicSource.volume > 0)
            {
                musicSource.volume -= startVolume * Time.deltaTime / fadeDuration;
                yield return null; //Waits a frame to do it again
            }
            musicSource.Stop();
            musicSource.clip = newClip;
            musicSource.loop = true;
            musicSource.Play();
            while (musicSource.volume < startVolume)
            {
                musicSource.volume += startVolume * Time.deltaTime / fadeDuration;
                yield return null; 
            }
            musicSource.volume = startVolume;
        }
        void AdvanceSeason()
        {
            CurrentSeason++;

            if((int)CurrentSeason > 3)
                CurrentSeason = Season.Spring;

            Debug.Log("Season changed to: " + CurrentSeason);

            PlaySeasonMusic();
        }
    }
}