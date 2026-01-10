using UnityEngine;

namespace Riftbourne.Audio
{
    /// <summary>
    /// Manages background music for the battle scene.
    /// Plays music on Start and loops it continuously.
    /// </summary>
    public class BattleMusicManager : MonoBehaviour
    {
        [Header("Background Music")]
        [SerializeField] private AudioClip battleMusic;
        [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.7f;
        [SerializeField] private bool playOnStart = true;

        private AudioSource musicSource;

        private void Awake()
        {
            // Create or get AudioSource component
            musicSource = GetComponent<AudioSource>();
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
            }

            // Configure AudioSource for music
            musicSource.clip = battleMusic;
            musicSource.loop = true; // Loop the music
            musicSource.playOnAwake = false; // We'll control when to play
            musicSource.volume = musicVolume;
            musicSource.priority = 0; // Lower priority than sound effects (default is 128)
        }

        private void Start()
        {
            if (playOnStart && battleMusic != null)
            {
                PlayMusic();
            }
        }

        /// <summary>
        /// Start playing the battle music.
        /// </summary>
        public void PlayMusic()
        {
            if (musicSource != null && battleMusic != null)
            {
                if (musicSource.clip != battleMusic)
                {
                    musicSource.clip = battleMusic;
                }
                musicSource.volume = musicVolume;
                musicSource.Play();
                Debug.Log($"BattleMusicManager: Started playing {battleMusic.name}");
            }
            else if (battleMusic == null)
            {
                Debug.LogWarning("BattleMusicManager: No battle music assigned!");
            }
        }

        /// <summary>
        /// Stop playing the battle music.
        /// </summary>
        public void StopMusic()
        {
            if (musicSource != null && musicSource.isPlaying)
            {
                musicSource.Stop();
                Debug.Log("BattleMusicManager: Stopped battle music");
            }
        }

        /// <summary>
        /// Set the music volume (0-1).
        /// </summary>
        public void SetVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (musicSource != null)
            {
                musicSource.volume = musicVolume;
            }
        }

        /// <summary>
        /// Fade out the music over a specified duration.
        /// </summary>
        public void FadeOut(float duration)
        {
            if (musicSource != null && musicSource.isPlaying)
            {
                StartCoroutine(FadeOutCoroutine(duration));
            }
        }

        private System.Collections.IEnumerator FadeOutCoroutine(float duration)
        {
            float startVolume = musicSource.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }

            musicSource.Stop();
            musicSource.volume = musicVolume; // Reset volume for next play
        }
    }
}
