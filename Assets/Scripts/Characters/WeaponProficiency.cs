using System;

namespace Riftbourne.Characters
{
    /// <summary>
    /// Tracks proficiency state for a single weapon family.
    /// Stores current tier and milestone progress toward next tier.
    /// Serializable for save/load system.
    /// </summary>
    [Serializable]
    public class WeaponProficiency
    {
        [UnityEngine.SerializeField] private WeaponFamily weaponFamily;
        [UnityEngine.SerializeField] private WeaponProficiencyTier currentTier;
        [UnityEngine.SerializeField] private int meaningfulHits;
        [UnityEngine.SerializeField] private int meaningfulKills;
        [UnityEngine.SerializeField] private int meaningfulCrits;

        public WeaponFamily WeaponFamily => weaponFamily;
        public WeaponProficiencyTier CurrentTier => currentTier;
        public int MeaningfulHits => meaningfulHits;
        public int MeaningfulKills => meaningfulKills;
        public int MeaningfulCrits => meaningfulCrits;

        public WeaponProficiency(WeaponFamily family)
        {
            weaponFamily = family;
            currentTier = WeaponProficiencyTier.Untrained;
            meaningfulHits = 0;
            meaningfulKills = 0;
            meaningfulCrits = 0;
        }

        /// <summary>
        /// Record a meaningful combat outcome and check for advancement.
        /// </summary>
        public void RecordCombatOutcome(bool wasHit, bool wasKill, bool wasCrit, bool isMeaningfulEncounter)
        {
            if (!isMeaningfulEncounter) return;

            if (wasHit) meaningfulHits++;
            if (wasKill) meaningfulKills++;
            if (wasCrit) meaningfulCrits++;

            CheckForAdvancement();
        }

        /// <summary>
        /// Check if enough milestones have been reached to advance to the next tier.
        /// Uses configurable thresholds from ProficiencySettings.
        /// </summary>
        private void CheckForAdvancement()
        {
            int totalOutcomes = meaningfulHits + meaningfulKills;
            ProficiencySettings settings = ProficiencySettings.Instance;

            switch (currentTier)
            {
                case WeaponProficiencyTier.Untrained:
                    if (meaningfulHits >= settings.UntrainedToFamiliarHits)
                    {
                        AdvanceTier();
                    }
                    break;

                case WeaponProficiencyTier.Familiar:
                    if (totalOutcomes >= settings.FamiliarToTrainedOutcomes)
                    {
                        AdvanceTier();
                    }
                    break;

                case WeaponProficiencyTier.Trained:
                    if (totalOutcomes >= settings.TrainedToCompetentOutcomes)
                    {
                        AdvanceTier();
                    }
                    break;

                case WeaponProficiencyTier.Competent:
                    if (totalOutcomes >= settings.CompetentToProficientOutcomes)
                    {
                        AdvanceTier();
                    }
                    break;

                case WeaponProficiencyTier.Proficient:
                    if (totalOutcomes >= settings.ProficientToAdvancedOutcomes)
                    {
                        AdvanceTier();
                    }
                    break;

                case WeaponProficiencyTier.Advanced:
                    if (totalOutcomes >= settings.AdvancedToExpertOutcomes && 
                        meaningfulCrits >= settings.AdvancedToExpertCrits)
                    {
                        AdvanceTier();
                    }
                    break;

                case WeaponProficiencyTier.Expert:
                    if (totalOutcomes >= settings.ExpertToMasterOutcomes && 
                        meaningfulCrits >= settings.ExpertToMasterCrits)
                    {
                        AdvanceTier();
                    }
                    break;

                case WeaponProficiencyTier.Master:
                    if (totalOutcomes >= settings.MasterToGrandmasterOutcomes && 
                        meaningfulCrits >= settings.MasterToGrandmasterCrits)
                    {
                        AdvanceTier();
                    }
                    break;

                case WeaponProficiencyTier.Grandmaster:
                    if (totalOutcomes >= settings.GrandmasterToLegendaryOutcomes && 
                        meaningfulCrits >= settings.GrandmasterToLegendaryCrits)
                    {
                        AdvanceTier();
                    }
                    break;

                case WeaponProficiencyTier.Legendary:
                    // Already at max tier
                    break;
            }
        }

        /// <summary>
        /// Advance to the next tier.
        /// </summary>
        private void AdvanceTier()
        {
            if (currentTier == WeaponProficiencyTier.Legendary) return;

            currentTier = (WeaponProficiencyTier)((int)currentTier + 1);
            UnityEngine.Debug.Log($"Weapon proficiency {weaponFamily} advanced to {currentTier}!");
        }

        /// <summary>
        /// Get total meaningful combat outcomes for display purposes.
        /// </summary>
        public int GetTotalOutcomes()
        {
            return meaningfulHits + meaningfulKills;
        }
    }
}
