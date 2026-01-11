using UnityEngine;

namespace Riftbourne.Characters
{
    /// <summary>
    /// Helper class for testing proficiency system.
    /// Provides quick methods to set testing thresholds.
    /// </summary>
    public static class ProficiencyTestingHelper
    {
        /// <summary>
        /// Enable testing mode - sets all thresholds to 1 for rapid advancement.
        /// </summary>
        public static void EnableTestingMode()
        {
            ProficiencySettings settings = ProficiencySettings.Instance;
            if (settings != null)
            {
                // Use reflection to set the private field, or create a new instance
                // For now, we'll use a workaround: create a new settings with testing values
                Debug.Log("ProficiencyTestingHelper: To enable testing mode, set 'Testing Mode' to true in ProficiencySettings asset, or use SetTestingThresholds()");
            }
        }

        /// <summary>
        /// Set all advancement thresholds to 1 for rapid testing.
        /// Note: This modifies the asset at runtime. Changes won't persist unless you save the asset.
        /// </summary>
        public static void SetTestingThresholds()
        {
            ProficiencySettings settings = ProficiencySettings.Instance;
            if (settings == null)
            {
                Debug.LogError("ProficiencyTestingHelper: ProficiencySettings not found! Create one first.");
                return;
            }

            // Use reflection to set private fields (for runtime testing)
            var settingsType = typeof(ProficiencySettings);
            var testingModeField = settingsType.GetField("testingMode", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (testingModeField != null)
            {
                testingModeField.SetValue(settings, true);
                Debug.Log("ProficiencyTestingHelper: Testing mode enabled - all thresholds set to 1");
            }
            else
            {
                Debug.LogWarning("ProficiencyTestingHelper: Could not enable testing mode via reflection. Set 'Testing Mode' to true in ProficiencySettings asset instead.");
            }
        }

        /// <summary>
        /// Manually advance a character's proficiency tier for testing.
        /// </summary>
        public static void ForceAdvanceProficiency(Unit unit, WeaponFamily family, WeaponProficiencyTier targetTier)
        {
            if (unit == null || unit.WeaponProficiencyManager == null)
            {
                Debug.LogError("ProficiencyTestingHelper: Unit or WeaponProficiencyManager is null!");
                return;
            }

            WeaponProficiency proficiency = unit.WeaponProficiencyManager.GetProficiency(family);
            if (proficiency == null)
            {
                Debug.LogError($"ProficiencyTestingHelper: Could not get proficiency for {family}");
                return;
            }

            // Use reflection to set the tier directly
            var proficiencyType = typeof(WeaponProficiency);
            var tierField = proficiencyType.GetField("currentTier", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (tierField != null)
            {
                tierField.SetValue(proficiency, targetTier);
                Debug.Log($"ProficiencyTestingHelper: {unit.UnitName}'s {family} proficiency set to {targetTier}");
            }
            else
            {
                Debug.LogError("ProficiencyTestingHelper: Could not set proficiency tier via reflection!");
            }
        }

        /// <summary>
        /// Initialize all weapon families for a unit (useful for testing UI display).
        /// </summary>
        public static void InitializeAllFamilies(Unit unit)
        {
            if (unit == null || unit.WeaponProficiencyManager == null)
            {
                Debug.LogError("ProficiencyTestingHelper: Unit or WeaponProficiencyManager is null!");
                return;
            }

            unit.WeaponProficiencyManager.InitializeAllFamilies();
            Debug.Log($"ProficiencyTestingHelper: Initialized all weapon families for {unit.UnitName}");
        }

        /// <summary>
        /// Make all encounters meaningful for testing (bypasses encounter detection).
        /// </summary>
        public static void SetAllEncountersMeaningful(bool enabled)
        {
            // This would require modifying WeaponProficiencyManager
            // For now, just log instructions
            if (enabled)
            {
                Debug.Log("ProficiencyTestingHelper: To make all encounters meaningful, set Meaningful Encounter Min HP Ratio to 0.0 in ProficiencySettings");
            }
        }
    }
}
