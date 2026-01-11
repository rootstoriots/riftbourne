using Riftbourne.Characters;

namespace Riftbourne.Combat
{
    /// <summary>
    /// Calculates tier-based modifiers for weapon proficiency.
    /// Provides stat efficiency, variance reduction, and handling bonuses.
    /// </summary>
    public static class ProficiencyEffects
    {
        /// <summary>
        /// Get the stat efficiency multiplier for a proficiency tier.
        /// Determines how much of Strength, Finesse, and Focus apply to combat.
        /// Scales from 50% (Untrained) to 150% (Legendary).
        /// </summary>
        public static float GetStatEfficiencyMultiplier(WeaponProficiencyTier tier)
        {
            switch (tier)
            {
                case WeaponProficiencyTier.Untrained:
                    return 0.5f;   // 50% stat efficiency
                case WeaponProficiencyTier.Familiar:
                    return 1.0f;   // 100% stat efficiency (baseline)
                case WeaponProficiencyTier.Trained:
                    return 1.05f;   // 105% stat efficiency
                case WeaponProficiencyTier.Competent:
                    return 1.1f;   // 110% stat efficiency
                case WeaponProficiencyTier.Proficient:
                    return 1.15f;  // 115% stat efficiency
                case WeaponProficiencyTier.Advanced:
                    return 1.2f;    // 120% stat efficiency
                case WeaponProficiencyTier.Expert:
                    return 1.25f;   // 125% stat efficiency
                case WeaponProficiencyTier.Master:
                    return 1.3f;    // 130% stat efficiency
                case WeaponProficiencyTier.Grandmaster:
                    return 1.4f;    // 140% stat efficiency
                case WeaponProficiencyTier.Legendary:
                    return 1.5f;    // 150% stat efficiency
                default:
                    return 1.0f;
            }
        }

        /// <summary>
        /// Get variance reduction bonus for hit chance.
        /// Reduces miss chance, making attacks more reliable.
        /// Scales from 0% (Untrained) to +15% (Legendary).
        /// </summary>
        public static float GetVarianceReduction(WeaponProficiencyTier tier)
        {
            switch (tier)
            {
                case WeaponProficiencyTier.Untrained:
                    return 0f;     // No bonus
                case WeaponProficiencyTier.Familiar:
                    return 0f;     // No bonus (baseline)
                case WeaponProficiencyTier.Trained:
                    return 1f;     // +1% hit chance
                case WeaponProficiencyTier.Competent:
                    return 2f;     // +2% hit chance
                case WeaponProficiencyTier.Proficient:
                    return 3.5f;   // +3.5% hit chance
                case WeaponProficiencyTier.Advanced:
                    return 5f;     // +5% hit chance
                case WeaponProficiencyTier.Expert:
                    return 7f;     // +7% hit chance
                case WeaponProficiencyTier.Master:
                    return 9f;     // +9% hit chance
                case WeaponProficiencyTier.Grandmaster:
                    return 12f;    // +12% hit chance
                case WeaponProficiencyTier.Legendary:
                    return 15f;    // +15% hit chance
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// Get fumble chance reduction.
        /// Reduces chance of critical failures or recovery delays.
        /// Scales from 0% (Untrained) to -20% (Legendary).
        /// </summary>
        public static float GetFumbleReduction(WeaponProficiencyTier tier)
        {
            switch (tier)
            {
                case WeaponProficiencyTier.Untrained:
                    return 0f;     // No reduction
                case WeaponProficiencyTier.Familiar:
                    return 0f;     // No reduction (baseline)
                case WeaponProficiencyTier.Trained:
                    return 1f;     // -1% fumble chance
                case WeaponProficiencyTier.Competent:
                    return 2f;     // -2% fumble chance
                case WeaponProficiencyTier.Proficient:
                    return 3.5f;   // -3.5% fumble chance
                case WeaponProficiencyTier.Advanced:
                    return 5f;     // -5% fumble chance
                case WeaponProficiencyTier.Expert:
                    return 7f;     // -7% fumble chance
                case WeaponProficiencyTier.Master:
                    return 10f;    // -10% fumble chance
                case WeaponProficiencyTier.Grandmaster:
                    return 15f;    // -15% fumble chance
                case WeaponProficiencyTier.Legendary:
                    return 20f;    // -20% fumble chance
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// Get handling bonus for recovery delays.
        /// Higher tiers reduce recovery time after actions.
        /// Scales from 0% (Untrained) to 30% (Legendary).
        /// </summary>
        public static float GetRecoveryBonus(WeaponProficiencyTier tier)
        {
            switch (tier)
            {
                case WeaponProficiencyTier.Untrained:
                    return 0f;     // No bonus
                case WeaponProficiencyTier.Familiar:
                    return 0f;     // No bonus (baseline)
                case WeaponProficiencyTier.Trained:
                    return 0.05f;  // 5% faster recovery
                case WeaponProficiencyTier.Competent:
                    return 0.1f;   // 10% faster recovery
                case WeaponProficiencyTier.Proficient:
                    return 0.15f;  // 15% faster recovery
                case WeaponProficiencyTier.Advanced:
                    return 0.18f;  // 18% faster recovery
                case WeaponProficiencyTier.Expert:
                    return 0.22f;  // 22% faster recovery
                case WeaponProficiencyTier.Master:
                    return 0.25f;  // 25% faster recovery
                case WeaponProficiencyTier.Grandmaster:
                    return 0.28f;  // 28% faster recovery
                case WeaponProficiencyTier.Legendary:
                    return 0.3f;   // 30% faster recovery
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// Get stamina efficiency bonus.
        /// Higher tiers use less stamina for actions.
        /// Scales from 0% (Untrained) to 30% (Legendary).
        /// </summary>
        public static float GetStaminaEfficiencyBonus(WeaponProficiencyTier tier)
        {
            switch (tier)
            {
                case WeaponProficiencyTier.Untrained:
                    return 0f;     // No bonus
                case WeaponProficiencyTier.Familiar:
                    return 0f;     // No bonus (baseline)
                case WeaponProficiencyTier.Trained:
                    return 0.05f;  // 5% less stamina cost
                case WeaponProficiencyTier.Competent:
                    return 0.1f;   // 10% less stamina cost
                case WeaponProficiencyTier.Proficient:
                    return 0.15f;  // 15% less stamina cost
                case WeaponProficiencyTier.Advanced:
                    return 0.18f;  // 18% less stamina cost
                case WeaponProficiencyTier.Expert:
                    return 0.22f;  // 22% less stamina cost
                case WeaponProficiencyTier.Master:
                    return 0.25f;  // 25% less stamina cost
                case WeaponProficiencyTier.Grandmaster:
                    return 0.28f;  // 28% less stamina cost
                case WeaponProficiencyTier.Legendary:
                    return 0.3f;   // 30% less stamina cost
                default:
                    return 0f;
            }
        }
    }
}
