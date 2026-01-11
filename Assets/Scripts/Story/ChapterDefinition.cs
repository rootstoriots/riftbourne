using UnityEngine;
using Riftbourne.Characters;
using System.Collections.Generic;

namespace Riftbourne.Story
{
    /// <summary>
    /// Defines a chapter in the game.
    /// Contains information about party composition, POV character, and progression requirements.
    /// </summary>
    [CreateAssetMenu(fileName = "New Chapter", menuName = "Riftbourne/Story/Chapter Definition")]
    public class ChapterDefinition : ScriptableObject
    {
        [Header("Chapter Identity")]
        [SerializeField] private string chapterID = "chapter_001";
        [SerializeField] private string chapterName = "New Chapter";
        [TextArea(3, 5)]
        [SerializeField] private string chapterDescription = "Chapter description";

        [Header("Narrative Track")]
        [Tooltip("Which storyline this chapter belongs to")]
        [SerializeField] private NarrativeTrack narrativeTrack;

        [Header("POV Character")]
        [Tooltip("Who is the protagonist for this chapter")]
        [SerializeField] private CharacterDefinition povCharacter;

        [Header("Party Composition")]
        [Tooltip("Characters that can be in the party for this chapter")]
        [SerializeField] private List<CharacterDefinition> availableCharacters = new List<CharacterDefinition>();
        [Tooltip("Characters that must be in the party for this chapter")]
        [SerializeField] private List<CharacterDefinition> requiredCharacters = new List<CharacterDefinition>();

        [Header("Starting Location")]
        [Tooltip("Scene name or location ID where exploration starts")]
        [SerializeField] private string startingLocation = "";

        [Header("Progression")]
        [Tooltip("What unlocks the next chapter (e.g., 'defeat_boss', 'complete_quest')")]
        [SerializeField] private List<string> progressionRequirements = new List<string>();
        [Tooltip("Next chapter in the track (if any)")]
        [SerializeField] private ChapterDefinition nextChapter;

        // Public properties
        public string ChapterID => chapterID;
        public string ChapterName => chapterName;
        public string ChapterDescription => chapterDescription;
        public NarrativeTrack NarrativeTrack => narrativeTrack;
        public CharacterDefinition POVCharacter => povCharacter;
        public List<CharacterDefinition> AvailableCharacters => new List<CharacterDefinition>(availableCharacters);
        public List<CharacterDefinition> RequiredCharacters => new List<CharacterDefinition>(requiredCharacters);
        public string StartingLocation => startingLocation;
        public List<string> ProgressionRequirements => new List<string>(progressionRequirements);
        public ChapterDefinition NextChapter => nextChapter;
    }
}
