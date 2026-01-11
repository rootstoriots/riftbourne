using UnityEngine;
using Riftbourne.Characters;
using System.Collections.Generic;

namespace Riftbourne.Story
{
    /// <summary>
    /// Defines a complete narrative storyline.
    /// Contains an ordered list of chapters and track-specific characters.
    /// </summary>
    [CreateAssetMenu(fileName = "New Narrative Track", menuName = "Riftbourne/Story/Narrative Track")]
    public class NarrativeTrack : ScriptableObject
    {
        [Header("Track Identity")]
        [SerializeField] private string trackID = "track_001";
        [SerializeField] private string trackName = "New Narrative Track";
        [TextArea(3, 5)]
        [SerializeField] private string trackDescription = "Track description";

        [Header("Chapters")]
        [Tooltip("Ordered list of chapters in this track")]
        [SerializeField] private List<ChapterDefinition> chapters = new List<ChapterDefinition>();

        [Header("Track-Specific Characters")]
        [Tooltip("Characters unique to this narrative track")]
        [SerializeField] private List<CharacterDefinition> trackSpecificCharacters = new List<CharacterDefinition>();

        // Public properties
        public string TrackID => trackID;
        public string TrackName => trackName;
        public string TrackDescription => trackDescription;
        public List<ChapterDefinition> Chapters => new List<ChapterDefinition>(chapters);
        public List<CharacterDefinition> TrackSpecificCharacters => new List<CharacterDefinition>(trackSpecificCharacters);

        /// <summary>
        /// Get the first chapter in this track.
        /// </summary>
        public ChapterDefinition GetFirstChapter()
        {
            return chapters != null && chapters.Count > 0 ? chapters[0] : null;
        }

        /// <summary>
        /// Get the next chapter after the given chapter.
        /// </summary>
        public ChapterDefinition GetNextChapter(ChapterDefinition currentChapter)
        {
            if (currentChapter == null || chapters == null) return null;

            int index = chapters.IndexOf(currentChapter);
            if (index >= 0 && index < chapters.Count - 1)
            {
                return chapters[index + 1];
            }

            return null;
        }

        /// <summary>
        /// Check if a chapter is in this track.
        /// </summary>
        public bool ContainsChapter(ChapterDefinition chapter)
        {
            return chapters != null && chapters.Contains(chapter);
        }
    }
}
