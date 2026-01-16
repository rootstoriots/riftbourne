using UnityEngine;

namespace Riftbourne.Exploration
{
    /// <summary>
    /// Represents a single dialogue entry with text, optional audio, weight, and duration.
    /// </summary>
    [System.Serializable]
    public class DialogueEntry
    {
        [Tooltip("The dialogue text to display (without speaker name - name is added automatically).")]
        [TextArea(2, 4)]
        public string dialogueText = "";

        [Tooltip("Optional speaker name override. If empty, uses the default speaker name from ProximityDialogueData.")]
        public string speakerNameOverride = "";

        [Tooltip("Optional audio clip for voice line. If null, no audio will play.")]
        public AudioClip audioClip;

        [Tooltip("Weight for weighted random selection. Higher values are more likely to be selected.")]
        [Range(0.1f, 10f)]
        public float weight = 1f;

        [Tooltip("How long the dialogue text stays visible (in seconds). If 0, uses default duration.")]
        [Range(0f, 30f)]
        public float displayDuration = 0f; // 0 means use default
    }

    /// <summary>
    /// ScriptableObject containing dialogue data for NPC proximity dialogue system.
    /// Create via: Assets/Create/Riftbourne/Proximity Dialogue Data
    /// </summary>
    [CreateAssetMenu(fileName = "New Proximity Dialogue", menuName = "Riftbourne/Proximity Dialogue Data", order = 1)]
    public class ProximityDialogueData : ScriptableObject
    {
        [Header("Speaker Settings")]
        [Tooltip("Default speaker name to display before dialogue (e.g., 'Dave'). Leave empty to hide speaker names.")]
        public string defaultSpeakerName = "";

        [Tooltip("Format for displaying speaker name. {0} = speaker name, {1} = dialogue text. Example: '{0}: {1}'")]
        public string speakerNameFormat = "{0}: {1}";

        [Header("Dialogue Settings")]
        [Tooltip("Array of dialogue entries. One will be randomly selected based on weights when player enters proximity.")]
        public DialogueEntry[] dialogueEntries = new DialogueEntry[1];

        [Header("Default Settings")]
        [Tooltip("Default display duration if dialogue entry doesn't specify one (in seconds).")]
        [Range(1f, 30f)]
        public float defaultDisplayDuration = 5f;

        /// <summary>
        /// Selects a dialogue entry using weighted random selection.
        /// </summary>
        /// <returns>The selected dialogue entry, or null if no entries exist.</returns>
        public DialogueEntry SelectRandomDialogue()
        {
            if (dialogueEntries == null || dialogueEntries.Length == 0)
            {
                Debug.LogWarning($"[ProximityDialogueData] {name}: No dialogue entries available!");
                return null;
            }

            // Calculate total weight
            float totalWeight = 0f;
            foreach (var entry in dialogueEntries)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.dialogueText))
                {
                    totalWeight += entry.weight;
                }
            }

            if (totalWeight <= 0f)
            {
                Debug.LogWarning($"[ProximityDialogueData] {name}: Total weight is 0 or negative!");
                return dialogueEntries[0]; // Fallback to first entry
            }

            // Generate random value
            float randomValue = Random.Range(0f, totalWeight);

            // Find entry by accumulating weights
            float accumulatedWeight = 0f;
            foreach (var entry in dialogueEntries)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.dialogueText))
                {
                    accumulatedWeight += entry.weight;
                    if (randomValue <= accumulatedWeight)
                    {
                        return entry;
                    }
                }
            }

            // Fallback (shouldn't reach here)
            return dialogueEntries[0];
        }

        /// <summary>
        /// Gets the display duration for a dialogue entry, using default if entry doesn't specify.
        /// </summary>
        public float GetDisplayDuration(DialogueEntry entry)
        {
            if (entry == null) return defaultDisplayDuration;
            return entry.displayDuration > 0f ? entry.displayDuration : defaultDisplayDuration;
        }

        /// <summary>
        /// Gets the formatted dialogue text with speaker name.
        /// </summary>
        public string GetFormattedDialogueText(DialogueEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.dialogueText))
                return "";

            // Get speaker name (override or default)
            string speakerName = string.IsNullOrEmpty(entry.speakerNameOverride) 
                ? defaultSpeakerName 
                : entry.speakerNameOverride;

            // If no speaker name, just return dialogue text
            if (string.IsNullOrEmpty(speakerName))
                return entry.dialogueText;

            // Format with speaker name
            return string.Format(speakerNameFormat, speakerName, entry.dialogueText);
        }

        /// <summary>
        /// Get all dialogue entries in order (for sequential dialogue).
        /// </summary>
        public DialogueEntry[] GetAllEntries()
        {
            return dialogueEntries;
        }
    }
}
