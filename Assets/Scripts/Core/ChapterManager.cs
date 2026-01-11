using UnityEngine;
using Riftbourne.Story;
using Riftbourne.Characters;
using System.Collections.Generic;

namespace Riftbourne.Core
{
    /// <summary>
    /// Manages current chapter and narrative track.
    /// Singleton pattern for easy access from UI and game systems.
    /// </summary>
    public class ChapterManager : MonoBehaviour
    {
        public static ChapterManager Instance { get; private set; }

        [Header("Current State")]
        [SerializeField] private ChapterDefinition currentChapter;
        [SerializeField] private NarrativeTrack currentTrack;
        [SerializeField] private Dictionary<string, bool> chapterProgressionState = new Dictionary<string, bool>();

        // Public properties
        public ChapterDefinition CurrentChapter => currentChapter;
        public NarrativeTrack CurrentTrack => currentTrack;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes
        }

        /// <summary>
        /// Load a chapter and set up the party.
        /// </summary>
        public bool LoadChapter(string chapterID)
        {
            if (string.IsNullOrEmpty(chapterID))
            {
                Debug.LogError("ChapterManager: Cannot load chapter with null or empty ID!");
                return false;
            }

            // Try to find chapter in current track first
            ChapterDefinition chapter = null;
            if (currentTrack != null)
            {
                foreach (var ch in currentTrack.Chapters)
                {
                    if (ch != null && ch.ChapterID == chapterID)
                    {
                        chapter = ch;
                        break;
                    }
                }
            }

            // If not found in current track, search all chapters (fallback)
            if (chapter == null)
            {
                chapter = Resources.Load<ChapterDefinition>($"Chapters/{chapterID}");
            }

            if (chapter == null)
            {
                Debug.LogError($"ChapterManager: Chapter {chapterID} not found!");
                return false;
            }

            return LoadChapter(chapter);
        }

        /// <summary>
        /// Load a chapter directly.
        /// </summary>
        public bool LoadChapter(ChapterDefinition chapter)
        {
            if (chapter == null)
            {
                Debug.LogError("ChapterManager: Cannot load null chapter!");
                return false;
            }

            currentChapter = chapter;
            currentTrack = chapter.NarrativeTrack;

            Debug.Log($"ChapterManager: Loading chapter {chapter.ChapterName} (ID: {chapter.ChapterID})");

            // Set up party from chapter definition
            SetupPartyFromChapter(chapter);

            // Mark chapter as started
            chapterProgressionState[chapter.ChapterID] = false; // false = started but not completed

            return true;
        }

        /// <summary>
        /// Set up party from chapter definition.
        /// </summary>
        private void SetupPartyFromChapter(ChapterDefinition chapter)
        {
            if (PartyManager.Instance == null)
            {
                Debug.LogWarning("ChapterManager: PartyManager not available! Cannot set up party.");
                return;
            }

            // Clear existing party
            PartyManager.Instance.ClearParty();

            // Add required characters first
            foreach (var charDef in chapter.RequiredCharacters)
            {
                if (charDef != null)
                {
                    CharacterState state = CharacterStateFactory.Create(charDef);
                    if (state != null)
                    {
                        PartyManager.Instance.AddPartyMember(state);
                    }
                }
            }

            // Set POV character
            if (chapter.POVCharacter != null)
            {
                // Find or create POV character state
                CharacterState povState = null;
                foreach (var member in PartyManager.Instance.GetPartyMembers())
                {
                    if (member.CharacterID == chapter.POVCharacter.CharacterID)
                    {
                        povState = member;
                        break;
                    }
                }

                if (povState == null)
                {
                    povState = CharacterStateFactory.Create(chapter.POVCharacter);
                    if (povState != null)
                    {
                        PartyManager.Instance.AddPartyMember(povState);
                    }
                }

                if (povState != null)
                {
                    PartyManager.Instance.SetPOVCharacter(povState);
                }
            }

            Debug.Log($"ChapterManager: Set up party with {PartyManager.Instance.GetPartyMembers().Count} members");
        }

        /// <summary>
        /// Get the current chapter.
        /// </summary>
        public ChapterDefinition GetCurrentChapter()
        {
            return currentChapter;
        }

        /// <summary>
        /// Get the current narrative track.
        /// </summary>
        public NarrativeTrack GetCurrentTrack()
        {
            return currentTrack;
        }

        /// <summary>
        /// Check if requirements are met to progress to the next chapter.
        /// </summary>
        public bool CanProgressToNextChapter()
        {
            if (currentChapter == null)
            {
                return false;
            }

            // Check if all progression requirements are met
            foreach (var requirement in currentChapter.ProgressionRequirements)
            {
                if (!string.IsNullOrEmpty(requirement))
                {
                    // Check if requirement is met (stored in progression state)
                    if (!chapterProgressionState.ContainsKey(requirement) || !chapterProgressionState[requirement])
                    {
                        return false;
                    }
                }
            }

            // Check if next chapter exists
            if (currentChapter.NextChapter == null && currentTrack != null)
            {
                // Try to get next chapter from track
                ChapterDefinition nextChapter = currentTrack.GetNextChapter(currentChapter);
                if (nextChapter == null)
                {
                    return false; // No next chapter
                }
            }

            return true;
        }

        /// <summary>
        /// Mark a progression requirement as completed.
        /// </summary>
        public void CompleteRequirement(string requirementID)
        {
            if (string.IsNullOrEmpty(requirementID)) return;

            chapterProgressionState[requirementID] = true;
            Debug.Log($"ChapterManager: Completed requirement {requirementID}");

            // Check if chapter can now progress
            if (CanProgressToNextChapter())
            {
                Debug.Log($"ChapterManager: All requirements met for chapter {currentChapter.ChapterName}!");
            }
        }

        /// <summary>
        /// Progress to the next chapter in the track.
        /// </summary>
        public bool ProgressToNextChapter()
        {
            if (!CanProgressToNextChapter())
            {
                Debug.LogWarning("ChapterManager: Cannot progress to next chapter - requirements not met!");
                return false;
            }

            if (currentChapter == null)
            {
                Debug.LogError("ChapterManager: Cannot progress - no current chapter!");
                return false;
            }

            // Mark current chapter as completed
            chapterProgressionState[currentChapter.ChapterID] = true;

            // Get next chapter
            ChapterDefinition nextChapter = null;
            if (currentChapter.NextChapter != null)
            {
                nextChapter = currentChapter.NextChapter;
            }
            else if (currentTrack != null)
            {
                nextChapter = currentTrack.GetNextChapter(currentChapter);
            }

            if (nextChapter == null)
            {
                Debug.LogWarning($"ChapterManager: No next chapter available for {currentChapter.ChapterName}");
                return false;
            }

            Debug.Log($"ChapterManager: Progressing from {currentChapter.ChapterName} to {nextChapter.ChapterName}");
            return LoadChapter(nextChapter);
        }

        /// <summary>
        /// Set the current narrative track.
        /// </summary>
        public void SetTrack(NarrativeTrack track)
        {
            if (track == null)
            {
                Debug.LogWarning("ChapterManager: Cannot set null track!");
                return;
            }

            currentTrack = track;
            Debug.Log($"ChapterManager: Set narrative track to {track.TrackName}");

            // Load first chapter of track if no chapter is loaded
            if (currentChapter == null)
            {
                ChapterDefinition firstChapter = track.GetFirstChapter();
                if (firstChapter != null)
                {
                    LoadChapter(firstChapter);
                }
            }
        }

        /// <summary>
        /// Check if a chapter has been completed.
        /// </summary>
        public bool IsChapterCompleted(string chapterID)
        {
            return chapterProgressionState.ContainsKey(chapterID) && chapterProgressionState[chapterID];
        }

        /// <summary>
        /// Get all completed chapters.
        /// </summary>
        public List<string> GetCompletedChapters()
        {
            List<string> completed = new List<string>();
            foreach (var kvp in chapterProgressionState)
            {
                if (kvp.Value)
                {
                    completed.Add(kvp.Key);
                }
            }
            return completed;
        }

        /// <summary>
        /// Get chapter progression state for save system.
        /// </summary>
        public Dictionary<string, bool> GetChapterProgressionState()
        {
            return new Dictionary<string, bool>(chapterProgressionState);
        }

        /// <summary>
        /// Set chapter progression state from save system.
        /// </summary>
        public void SetChapterProgressionState(Dictionary<string, bool> progressionState)
        {
            if (progressionState != null)
            {
                chapterProgressionState = new Dictionary<string, bool>(progressionState);
            }
        }
    }
}
