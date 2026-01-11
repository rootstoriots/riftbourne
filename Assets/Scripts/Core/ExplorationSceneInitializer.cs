using UnityEngine;
using Riftbourne.Story;

namespace Riftbourne.Core
{
    /// <summary>
    /// Initializes the exploration scene.
    /// Automatically loads the first chapter if no party exists.
    /// Attach this to a GameObject in your exploration scene.
    /// </summary>
    public class ExplorationSceneInitializer : MonoBehaviour
    {
        [Header("Auto-Load Settings")]
        [Tooltip("If true, automatically load first chapter when scene starts (if no party exists)")]
        [SerializeField] private bool autoLoadOnStart = true;
        
        [Tooltip("The narrative track to use if no track is currently set")]
        [SerializeField] private NarrativeTrack defaultTrack;
        
        [Tooltip("If true, only auto-load if party is empty")]
        [SerializeField] private bool onlyLoadIfPartyEmpty = true;

        [Header("Debug")]
        [Tooltip("Enable debug logging")]
        [SerializeField] private bool debugLogging = true;

        private void Start()
        {
            if (autoLoadOnStart)
            {
                InitializeExplorationScene();
            }
        }

        /// <summary>
        /// Initialize the exploration scene with party and chapter.
        /// </summary>
        public void InitializeExplorationScene()
        {
            // Check if party already exists
            if (onlyLoadIfPartyEmpty && PartyManager.Instance != null)
            {
                var partyMembers = PartyManager.Instance.GetPartyMembers();
                if (partyMembers != null && partyMembers.Count > 0)
                {
                    if (debugLogging)
                    {
                        Debug.Log($"ExplorationSceneInitializer: Party already has {partyMembers.Count} members. Skipping auto-load.");
                    }
                    return;
                }
            }

            // Check if ChapterManager has a track set
            if (ChapterManager.Instance != null)
            {
                NarrativeTrack currentTrack = ChapterManager.Instance.GetCurrentTrack();
                
                if (currentTrack == null)
                {
                    // No track set, use default if available
                    if (defaultTrack != null)
                    {
                        if (debugLogging)
                        {
                            Debug.Log($"ExplorationSceneInitializer: No track set, using default track '{defaultTrack.TrackName}'");
                        }
                        ChapterManager.Instance.SetTrack(defaultTrack);
                        currentTrack = defaultTrack;
                    }
                    else
                    {
                        if (debugLogging)
                        {
                            Debug.LogWarning("ExplorationSceneInitializer: No track set and no default track assigned!");
                        }
                        return;
                    }
                }

                // Check if chapter is already loaded
                ChapterDefinition currentChapter = ChapterManager.Instance.GetCurrentChapter();
                if (currentChapter == null && currentTrack != null)
                {
                    // Load first chapter of track
                    ChapterDefinition firstChapter = currentTrack.GetFirstChapter();
                    if (firstChapter != null)
                    {
                        if (debugLogging)
                        {
                            Debug.Log($"ExplorationSceneInitializer: Auto-loading first chapter '{firstChapter.ChapterName}' from track '{currentTrack.TrackName}'");
                        }
                        ChapterManager.Instance.LoadChapter(firstChapter);
                    }
                    else
                    {
                        Debug.LogError($"ExplorationSceneInitializer: Track '{currentTrack.TrackName}' has no chapters!");
                    }
                }
                else if (currentChapter != null)
                {
                    if (debugLogging)
                    {
                        Debug.Log($"ExplorationSceneInitializer: Chapter '{currentChapter.ChapterName}' already loaded. Party should be set up.");
                    }
                }
            }
            else
            {
                Debug.LogError("ExplorationSceneInitializer: ChapterManager.Instance is null! Make sure ChapterManager exists in the scene or persists across scenes.");
            }
        }

        /// <summary>
        /// Manually trigger initialization (useful for testing or button presses).
        /// </summary>
        public void ManualInitialize()
        {
            InitializeExplorationScene();
        }
    }
}
