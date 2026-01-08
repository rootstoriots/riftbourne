using UnityEngine;
using Riftbourne.Characters;

namespace Riftbourne.Combat.AI
{
    /// <summary>
    /// ScriptableObject data container for AI behavior configuration.
    /// Allows designers to configure behavior parameters without code changes.
    /// </summary>
    [CreateAssetMenu(fileName = "New AI Behavior", menuName = "Riftbourne/AI Behavior")]
    public class AIBehaviorData : ScriptableObject
    {
        [Header("Behavior Type")]
        [Tooltip("The type of AI behavior this data represents")]
        [SerializeField] private AIBehaviorType behaviorType = AIBehaviorType.Berserker;

        [Header("Target Selection")]
        [Tooltip("Weight for preferring low HP targets (0-1)")]
        [SerializeField] [Range(0f, 1f)] private float lowHPWeight = 0.5f;
        [Tooltip("Weight for preferring close targets (0-1)")]
        [SerializeField] [Range(0f, 1f)] private float proximityWeight = 0.3f;
        [Tooltip("Weight for preferring high threat targets (0-1)")]
        [SerializeField] [Range(0f, 1f)] private float threatWeight = 0.2f;

        [Header("Movement")]
        [Tooltip("How aggressively this unit moves toward enemies (0-1)")]
        [SerializeField] [Range(0f, 1f)] private float aggressionLevel = 0.7f;
        [Tooltip("How much this unit avoids hazards (0-1)")]
        [SerializeField] [Range(0f, 1f)] private float hazardAvoidance = 0.5f;

        [Header("Action Preferences")]
        [Tooltip("Preference for using skills over basic attacks (0-1)")]
        [SerializeField] [Range(0f, 1f)] private float skillPreference = 0.3f;
        [Tooltip("Preference for supporting allies vs attacking enemies (0-1, higher = more support)")]
        [SerializeField] [Range(0f, 1f)] private float supportPreference = 0.0f;
        [Tooltip("Minimum HP percentage before considering retreat (0-1)")]
        [SerializeField] [Range(0f, 1f)] private float retreatThreshold = 0.3f;

        // Public properties
        public AIBehaviorType BehaviorType => behaviorType;
        public float LowHPWeight => lowHPWeight;
        public float ProximityWeight => proximityWeight;
        public float ThreatWeight => threatWeight;
        public float AggressionLevel => aggressionLevel;
        public float HazardAvoidance => hazardAvoidance;
        public float SkillPreference => skillPreference;
        public float SupportPreference => supportPreference;
        public float RetreatThreshold => retreatThreshold;
    }

    /// <summary>
    /// Enum for different AI behavior types.
    /// </summary>
    public enum AIBehaviorType
    {
        Berserker,  // Aggressive, attacks closest/low HP targets
        Support,    // Heals/buffs allies, attacks when safe
        Coward,     // Defensive, retreats when low HP
        Protector   // Tanks, protects allies, draws aggro
    }
}

