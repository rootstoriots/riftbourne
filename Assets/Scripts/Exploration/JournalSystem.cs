using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Riftbourne.Exploration
{
    /// <summary>
    /// Singleton manager for journal entries.
    /// Handles adding, storing, and filtering journal entries during exploration.
    /// </summary>
    public class JournalSystem : MonoBehaviour
    {
        private static JournalSystem instance;
        
        public static JournalSystem Instance
        {
            get
            {
                if (instance == null)
                {
                    // Try to find existing instance
                    instance = FindFirstObjectByType<JournalSystem>();
                    
                    // If not found, create a new GameObject with JournalSystem
                    if (instance == null)
                    {
                        GameObject journalSystemObj = new GameObject("JournalSystem");
                        instance = journalSystemObj.AddComponent<JournalSystem>();
                        DontDestroyOnLoad(journalSystemObj);
                    }
                }
                return instance;
            }
        }
        
        private List<JournalEntry> entries = new List<JournalEntry>();
        
        /// <summary>
        /// Get all journal entries.
        /// </summary>
        public List<JournalEntry> GetAllEntries()
        {
            return new List<JournalEntry>(entries);
        }
        
        /// <summary>
        /// Get entries filtered by confidence level.
        /// </summary>
        public List<JournalEntry> GetEntriesByConfidence(ConfidenceLevel level)
        {
            return entries.Where(e => e.ConfidenceLevel == level).ToList();
        }
        
        /// <summary>
        /// Get unresolved entries.
        /// </summary>
        public List<JournalEntry> GetUnresolvedEntries()
        {
            return entries.Where(e => e.IsUnresolved).ToList();
        }
        
        /// <summary>
        /// Add a new journal entry.
        /// </summary>
        public void AddEntry(string entryText, ConfidenceLevel confidenceLevel, List<string> relatedSymbols = null)
        {
            if (string.IsNullOrEmpty(entryText))
            {
                Debug.LogWarning("JournalSystem: Attempted to add empty journal entry.");
                return;
            }
            
            JournalEntry entry = new JournalEntry(entryText, confidenceLevel, relatedSymbols: relatedSymbols);
            entries.Add(entry);
            
            Debug.Log($"[Journal] Added entry: {entryText} (Confidence: {confidenceLevel})");
        }
        
        /// <summary>
        /// Add a journal entry with full options.
        /// </summary>
        public void AddEntry(
            string entryText,
            ConfidenceLevel confidenceLevel,
            bool isUnresolved,
            List<string> relatedSymbols = null,
            bool canBeRecontextualized = true,
            bool isKnownIncorrect = false)
        {
            if (string.IsNullOrEmpty(entryText))
            {
                Debug.LogWarning("JournalSystem: Attempted to add empty journal entry.");
                return;
            }
            
            JournalEntry entry = new JournalEntry(
                entryText,
                confidenceLevel,
                isUnresolved,
                relatedSymbols,
                canBeRecontextualized,
                isKnownIncorrect
            );
            entries.Add(entry);
            
            Debug.Log($"[Journal] Added entry: {entryText} (Confidence: {confidenceLevel}, Unresolved: {isUnresolved})");
        }
        
        /// <summary>
        /// Clear all journal entries (for testing or reset).
        /// </summary>
        public void ClearEntries()
        {
            entries.Clear();
            Debug.Log("[Journal] All entries cleared.");
        }
        
        /// <summary>
        /// Get the total number of entries.
        /// </summary>
        public int EntryCount => entries.Count;
        
        private void Awake()
        {
            // Ensure singleton pattern
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
