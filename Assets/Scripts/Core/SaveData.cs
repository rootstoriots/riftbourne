using UnityEngine;
using Riftbourne.Characters;
using Riftbourne.Exploration;
using System;
using System.Collections.Generic;

namespace Riftbourne.Core
{
    /// <summary>
    /// Serializable data container for game state.
    /// Contains all information needed to restore the game to a saved state.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        [Header("Metadata")]
        public string saveName;
        public SaveType saveType;
        public string timestamp;
        public string version = "1.0"; // For future compatibility

        [Header("Chapter Data")]
        public string currentChapterID;
        public string currentChapterName;
        public string currentTrackName;
        public List<ChapterProgressionEntry> chapterProgressionState = new List<ChapterProgressionEntry>();

        [Header("Party Data")]
        public List<CharacterState> partyMembers = new List<CharacterState>();
        public string povCharacterID;

        [Header("Journal Data")]
        public List<SerializableJournalEntry> journalEntries = new List<SerializableJournalEntry>();

        [Header("Scene Data")]
        public string currentSceneName;
        public Vector3 playerPosition;

        [Header("Screenshot")]
        public string screenshotPath; // Relative to save directory

        /// <summary>
        /// Create SaveData from current game state.
        /// </summary>
        public static SaveData CreateFromCurrentState(string saveName, SaveType saveType)
        {
            SaveData data = new SaveData();
            data.saveName = saveName;
            data.saveType = saveType;
            data.timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Collect chapter data
            if (ChapterManager.Instance != null)
            {
                var chapter = ChapterManager.Instance.CurrentChapter;
                if (chapter != null)
                {
                    data.currentChapterID = chapter.ChapterID;
                    data.currentChapterName = chapter.ChapterName;
                }

                var track = ChapterManager.Instance.CurrentTrack;
                if (track != null)
                {
                    data.currentTrackName = track.TrackName;
                }

                // Serialize chapter progression state
                var progressionState = ChapterManager.Instance.GetChapterProgressionState();
                foreach (var kvp in progressionState)
                {
                    data.chapterProgressionState.Add(new ChapterProgressionEntry(kvp.Key, kvp.Value));
                }
            }

            // Collect party data
            if (PartyManager.Instance != null)
            {
                data.partyMembers = new List<CharacterState>(PartyManager.Instance.GetPartyMembers());
                var povCharacter = PartyManager.Instance.POVCharacter;
                if (povCharacter != null)
                {
                    data.povCharacterID = povCharacter.CharacterID;
                }
            }

            // Collect journal entries
            if (JournalSystem.Instance != null)
            {
                var entries = JournalSystem.Instance.GetAllEntries();
                foreach (var entry in entries)
                {
                    data.journalEntries.Add(new SerializableJournalEntry(entry));
                }
            }

            // Collect scene data
            data.currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            
            // Get player position from ExplorationController
            var explorationController = UnityEngine.Object.FindFirstObjectByType<ExplorationController>();
            if (explorationController != null)
            {
                data.playerPosition = explorationController.transform.position;
                Debug.Log($"SaveData: Saved player position: {data.playerPosition} (X:{data.playerPosition.x:F2}, Y:{data.playerPosition.y:F2}, Z:{data.playerPosition.z:F2})");
            }
            else
            {
                Debug.LogWarning("SaveData: Could not find ExplorationController to save position!");
                data.playerPosition = Vector3.zero;
            }

            return data;
        }
    }

    /// <summary>
    /// Save type enum.
    /// </summary>
    public enum SaveType
    {
        Autosave,
        Quicksave,
        Manual
    }

    /// <summary>
    /// Serializable entry for chapter progression state.
    /// </summary>
    [Serializable]
    public class ChapterProgressionEntry
    {
        public string chapterID;
        public bool isCompleted;

        public ChapterProgressionEntry(string id, bool completed)
        {
            chapterID = id;
            isCompleted = completed;
        }
    }

    /// <summary>
    /// Serializable version of JournalEntry for save/load.
    /// </summary>
    [Serializable]
    public class SerializableJournalEntry
    {
        public string entryText;
        public ConfidenceLevel confidenceLevel;
        public bool isUnresolved;
        public string timestamp;
        public List<string> relatedSymbols;
        public bool canBeRecontextualized;
        public bool isKnownIncorrect;

        public SerializableJournalEntry(JournalEntry entry)
        {
            entryText = entry.EntryText;
            confidenceLevel = entry.ConfidenceLevel;
            isUnresolved = entry.IsUnresolved;
            timestamp = entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
            relatedSymbols = new List<string>(entry.RelatedSymbols);
            canBeRecontextualized = entry.CanBeRecontextualized;
            isKnownIncorrect = entry.IsKnownIncorrect;
        }

        /// <summary>
        /// Convert back to JournalEntry.
        /// </summary>
        public JournalEntry ToJournalEntry()
        {
            DateTime entryTime;
            if (!DateTime.TryParse(timestamp, out entryTime))
            {
                entryTime = DateTime.Now;
            }

            // JournalEntry constructor doesn't accept timestamp, so we'll create it and use reflection or add a constructor
            // For now, we'll create a new entry with current time (timestamp is mainly for display)
            var entry = new JournalEntry(
                entryText,
                confidenceLevel,
                isUnresolved,
                relatedSymbols,
                canBeRecontextualized,
                isKnownIncorrect
            );

            return entry;
        }
    }
}
