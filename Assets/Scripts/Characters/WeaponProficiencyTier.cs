namespace Riftbourne.Characters
{
    /// <summary>
    /// Defines the ten tiers of weapon proficiency.
    /// Represents practical familiarity and combat reliability with a weapon family.
    /// </summary>
    public enum WeaponProficiencyTier
    {
        /// <summary>
        /// Inefficient handling, reduced stat application, higher risk under pressure.
        /// </summary>
        Untrained = 0,

        /// <summary>
        /// Basic familiarity; penalties removed; weapon functions as expected.
        /// </summary>
        Familiar = 1,

        /// <summary>
        /// Minor reliability bonuses and smoother execution.
        /// </summary>
        Trained = 2,

        /// <summary>
        /// Improved consistency and better stat expression.
        /// </summary>
        Competent = 3,

        /// <summary>
        /// Solid proficiency with noticeable advantages.
        /// </summary>
        Proficient = 4,

        /// <summary>
        /// Advanced skill with significant bonuses.
        /// </summary>
        Advanced = 5,

        /// <summary>
        /// Expert level with major advantages.
        /// </summary>
        Expert = 6,

        /// <summary>
        /// Master level with exceptional bonuses.
        /// </summary>
        Master = 7,

        /// <summary>
        /// Grandmaster level with maximum bonuses.
        /// </summary>
        Grandmaster = 8,

        /// <summary>
        /// Legendary level - peak proficiency with unique advantages.
        /// </summary>
        Legendary = 9
    }
}
