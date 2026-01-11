using UnityEngine;
using Riftbourne.Story;
using Riftbourne.Characters;

namespace Riftbourne.Core
{
    /// <summary>
    /// Initializes the game on startup.
    /// Sets up the starting narrative track and loads the first chapter.
    /// Attach this to a GameObject in your first scene (menu or exploration).
    /// </summary>
    public class GameInitializer : MonoBehaviour
    {
        [Header("Starting Configuration")]
        [Tooltip("The narrative track to start the game with")]
        [SerializeField] private NarrativeTrack startingTrack;
        
        [Tooltip("If true, automatically load the first chapter when track is set")]
        [SerializeField] private bool autoLoadFirstChapter = true;
        
        [Tooltip("If true, initialize even if ChapterManager already has a track set (useful for testing)")]
        [SerializeField] private bool forceInitialize = false;

        [Header("Debug")]
        [Tooltip("Enable debug logging for initialization")]
        [SerializeField] private bool debugLogging = true;

        private void Start()
        {
            InitializeGame();
        }

        /// <summary>
        /// Initialize the game with the starting track and chapter.
        /// </summary>
        public void InitializeGame()
        {
            if (startingTrack == null)
            {
                if (debugLogging)
                {
                    Debug.LogWarning("GameInitializer: No starting track assigned! Game will not initialize chapter system.");
                }
                return;
            }

            // Check if ChapterManager already has a track (don't reinitialize unless forced)
            if (!forceInitialize && ChapterManager.Instance != null && ChapterManager.Instance.GetCurrentTrack() != null)
            {
                if (debugLogging)
                {
                    Debug.Log("GameInitializer: ChapterManager already has a track set. Skipping initialization.");
                }
                return;
            }

            if (debugLogging)
            {
                Debug.Log($"GameInitializer: Initializing game with track '{startingTrack.TrackName}' (ID: {startingTrack.TrackID})");
            }

            // Set the narrative track
            if (ChapterManager.Instance != null)
            {
                ChapterManager.Instance.SetTrack(startingTrack);

                // Auto-load first chapter if enabled
                if (autoLoadFirstChapter)
                {
                    ChapterDefinition firstChapter = startingTrack.GetFirstChapter();
                    if (firstChapter != null)
                    {
                        if (debugLogging)
                        {
                            Debug.Log($"GameInitializer: Auto-loading first chapter '{firstChapter.ChapterName}' (ID: {firstChapter.ChapterID})");
                        }
                        ChapterManager.Instance.LoadChapter(firstChapter);
                    }
                    else
                    {
                        Debug.LogError($"GameInitializer: Starting track '{startingTrack.TrackName}' has no chapters!");
                    }
                }
            }
            else
            {
                Debug.LogError("GameInitializer: ChapterManager.Instance is null! Make sure ChapterManager exists in the scene.");
            }
        }

        /// <summary>
        /// Manually set a different starting track (useful for testing or menu selection).
        /// </summary>
        public void SetStartingTrack(NarrativeTrack track)
        {
            if (track == null)
            {
                Debug.LogWarning("GameInitializer: Cannot set null track!");
                return;
            }

            startingTrack = track;
            
            if (ChapterManager.Instance != null)
            {
                ChapterManager.Instance.SetTrack(track);
                
                if (autoLoadFirstChapter)
                {
                    ChapterDefinition firstChapter = track.GetFirstChapter();
                    if (firstChapter != null)
                    {
                        ChapterManager.Instance.LoadChapter(firstChapter);
                    }
                }
            }
        }

        /// <summary>
        /// Load a specific chapter by ID (useful for testing or chapter selection).
        /// </summary>
        public void LoadChapter(string chapterID)
        {
            if (ChapterManager.Instance != null)
            {
                ChapterManager.Instance.LoadChapter(chapterID);
            }
            else
            {
                Debug.LogError("GameInitializer: ChapterManager.Instance is null!");
            }
        }
    }
}
