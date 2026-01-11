using UnityEngine;

namespace Riftbourne.Skills
{
    /// <summary>
    /// ScriptableObject defining a narrative skill for exploration mode.
    /// Narrative skills are perceptual filters that modify how the world reveals itself.
    /// They are not combat abilities - they unlock perception layers during exploration.
    /// </summary>
    [CreateAssetMenu(fileName = "New Narrative Skill", menuName = "Riftbourne/Skills/Narrative Skill")]
    public class NarrativeSkill : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string skillName = "New Narrative Skill";
        
        [SerializeField] private NarrativeSkillCategory category = NarrativeSkillCategory.Perception;
        
        [TextArea(3, 5)]
        [SerializeField] private string description = "Skill description";
        
        [Header("Threshold Bands")]
        [Tooltip("Skill level 0-2: Nothing surfaced")]
        [SerializeField] private int minimalHint = 3;
        
        [Tooltip("Skill level 3-5: Vague hint")]
        [SerializeField] private int structuredClue = 6;
        
        [Tooltip("Skill level 6-8: Structured clue")]
        [SerializeField] private int multipleTheories = 9;
        
        // Public properties
        public string SkillName => skillName;
        public NarrativeSkillCategory Category => category;
        public string Description => description;
        public int MinimalHint => minimalHint;
        public int StructuredClue => structuredClue;
        public int MultipleTheories => multipleTheories;
        
        /// <summary>
        /// Get the threshold band for a given skill level.
        /// Returns which band the level falls into.
        /// </summary>
        public ThresholdBand GetThresholdBand(int skillLevel)
        {
            if (skillLevel >= multipleTheories)
                return ThresholdBand.MultipleTheories;
            if (skillLevel >= structuredClue)
                return ThresholdBand.StructuredClue;
            if (skillLevel >= minimalHint)
                return ThresholdBand.MinimalHint;
            return ThresholdBand.None;
        }
    }
    
    /// <summary>
    /// Threshold bands for narrative skill levels.
    /// </summary>
    public enum ThresholdBand
    {
        None,              // 0-2: Nothing surfaced
        MinimalHint,       // 3-5: Vague hint
        StructuredClue,    // 6-8: Structured clue
        MultipleTheories   // 9+: Multiple interpretations
    }
}
