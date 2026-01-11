namespace Riftbourne.Skills
{
    /// <summary>
    /// Categories of narrative skills used during exploration mode.
    /// These skills modify how the world reveals itself, not combat abilities.
    /// </summary>
    public enum NarrativeSkillCategory
    {
        /// <summary>
        /// Awareness, observation, pattern recognition.
        /// Adds metadata to locations, objects, NPC behaviors.
        /// Surfaces environmental inconsistencies.
        /// </summary>
        Perception,
        
        /// <summary>
        /// Lore literacy, symbol decoding, cultural memory.
        /// Converts raw observations into meaning.
        /// Unlocks speculative interpretations.
        /// </summary>
        Interpretive,
        
        /// <summary>
        /// Social intuition, intent sense, emotional residue reading.
        /// Evaluates spaces and remnants, not live dialogue.
        /// Infers past motivations, tensions, or secrets.
        /// </summary>
        Empathic
    }
}
