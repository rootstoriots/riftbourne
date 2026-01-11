using System;
using System.Collections.Generic;

namespace Riftbourne.Exploration
{
    /// <summary>
    /// Immutable data class representing a single journal entry.
    /// Journal entries capture observations, interpretations, and theories during exploration.
    /// </summary>
    public class JournalEntry
    {
        public string EntryText { get; }
        public ConfidenceLevel ConfidenceLevel { get; }
        public bool IsUnresolved { get; }
        public DateTime Timestamp { get; }
        public List<string> RelatedSymbols { get; }
        public bool CanBeRecontextualized { get; }
        public bool IsKnownIncorrect { get; }
        
        /// <summary>
        /// Create a new journal entry.
        /// </summary>
        public JournalEntry(
            string entryText,
            ConfidenceLevel confidenceLevel,
            bool isUnresolved = false,
            List<string> relatedSymbols = null,
            bool canBeRecontextualized = true,
            bool isKnownIncorrect = false)
        {
            EntryText = entryText ?? string.Empty;
            ConfidenceLevel = confidenceLevel;
            IsUnresolved = isUnresolved;
            Timestamp = DateTime.Now;
            RelatedSymbols = relatedSymbols ?? new List<string>();
            CanBeRecontextualized = canBeRecontextualized;
            IsKnownIncorrect = isKnownIncorrect;
        }
        
        /// <summary>
        /// Create a copy of this entry with modified properties.
        /// Used for recontextualization or marking as incorrect.
        /// </summary>
        public JournalEntry WithModifications(
            string newText = null,
            ConfidenceLevel? newConfidence = null,
            bool? newIsUnresolved = null,
            List<string> newSymbols = null,
            bool? newCanBeRecontextualized = null,
            bool? newIsKnownIncorrect = null)
        {
            return new JournalEntry(
                newText ?? EntryText,
                newConfidence ?? ConfidenceLevel,
                newIsUnresolved ?? IsUnresolved,
                newSymbols ?? RelatedSymbols,
                newCanBeRecontextualized ?? CanBeRecontextualized,
                newIsKnownIncorrect ?? IsKnownIncorrect
            );
        }
    }
}
