using Riftbourne.Characters;

namespace Riftbourne.Items
{
    /// <summary>
    /// Represents a single effect that a consumable item can apply.
    /// Serializable struct for inspector visibility.
    /// </summary>
    [System.Serializable]
    public struct ConsumableEffect
    {
        /// <summary>
        /// The type of effect to apply.
        /// </summary>
        public ConsumableEffectType effectType;

        /// <summary>
        /// The magnitude of the effect (damage amount, heal amount, stat bonus, etc.).
        /// </summary>
        public int magnitude;

        /// <summary>
        /// Duration in turns/rounds. 0 means instant effect.
        /// </summary>
        public int duration;

        /// <summary>
        /// The stat affected by buff/debuff effects.
        /// ONLY used for BuffStat and DebuffStat effect types.
        /// IGNORED for Heal, Damage, RemoveStatusEffect, and ApplyStatusEffect.
        /// 
        /// For Heal effects: This field is ignored. Heal always restores CURRENT HP.
        /// For Damage effects: This field is ignored. Damage always reduces CURRENT HP.
        /// For BuffStat/DebuffStat: Use this to specify which stat to modify (Strength, MaxHP, etc.).
        /// </summary>
        public StatType affectedStat;

        /// <summary>
        /// Area of effect radius in grid cells.
        /// Only relevant for GroundAOE target types.
        /// </summary>
        public int aoeRadius;
    }
}
