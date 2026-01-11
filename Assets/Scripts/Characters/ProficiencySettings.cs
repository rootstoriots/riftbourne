using UnityEngine;

namespace Riftbourne.Characters
{
    /// <summary>
    /// Configurable settings for weapon proficiency advancement.
    /// Create an instance via Assets > Create > Riftbourne > Proficiency Settings
    /// Place in Resources folder to be auto-loaded, or assign directly.
    /// </summary>
    [CreateAssetMenu(fileName = "ProficiencySettings", menuName = "Riftbourne/Proficiency Settings")]
    public class ProficiencySettings : ScriptableObject
    {
        [Header("Testing Mode")]
        [Tooltip("Enable testing mode for rapid proficiency advancement (1 hit/outcome per tier)")]
        [SerializeField] private bool testingMode = false;
        
        [Header("Tier Advancement Thresholds")]
        [Tooltip("Number of meaningful hits required to advance from Untrained to Familiar")]
        [SerializeField] private int untrainedToFamiliarHits = 3;
        
        [Tooltip("Number of meaningful combat outcomes (hits + kills) required to advance from Familiar to Trained")]
        [SerializeField] private int familiarToTrainedOutcomes = 5;
        
        [Tooltip("Number of meaningful combat outcomes required to advance from Trained to Competent")]
        [SerializeField] private int trainedToCompetentOutcomes = 8;
        
        [Tooltip("Number of meaningful combat outcomes required to advance from Competent to Proficient")]
        [SerializeField] private int competentToProficientOutcomes = 12;
        
        [Tooltip("Number of meaningful combat outcomes required to advance from Proficient to Advanced")]
        [SerializeField] private int proficientToAdvancedOutcomes = 15;
        
        [Tooltip("Number of meaningful combat outcomes required to advance from Advanced to Expert")]
        [SerializeField] private int advancedToExpertOutcomes = 20;
        
        [Tooltip("Number of meaningful critical hits required to advance from Advanced to Expert")]
        [SerializeField] private int advancedToExpertCrits = 2;
        
        [Tooltip("Number of meaningful combat outcomes required to advance from Expert to Master")]
        [SerializeField] private int expertToMasterOutcomes = 25;
        
        [Tooltip("Number of meaningful critical hits required to advance from Expert to Master")]
        [SerializeField] private int expertToMasterCrits = 3;
        
        [Tooltip("Number of meaningful combat outcomes required to advance from Master to Grandmaster")]
        [SerializeField] private int masterToGrandmasterOutcomes = 30;
        
        [Tooltip("Number of meaningful critical hits required to advance from Master to Grandmaster")]
        [SerializeField] private int masterToGrandmasterCrits = 5;
        
        [Tooltip("Number of meaningful combat outcomes required to advance from Grandmaster to Legendary")]
        [SerializeField] private int grandmasterToLegendaryOutcomes = 40;
        
        [Tooltip("Number of meaningful critical hits required to advance from Grandmaster to Legendary")]
        [SerializeField] private int grandmasterToLegendaryCrits = 8;

        [Header("Meaningful Encounter Detection")]
        [Tooltip("Target must have at least this percentage of attacker's max HP to be meaningful (0.5 = 50%)")]
        [Range(0f, 1f)]
        [SerializeField] private float meaningfulEncounterMinHPRatio = 0.5f;
        
        [Tooltip("If target's attack power is below this AND HP is below threshold, encounter is trivial (0.6 = 60%)")]
        [Range(0f, 1f)]
        [SerializeField] private float trivialEncounterAttackRatio = 0.6f;
        
        [Tooltip("If target's HP is below this AND attack is below threshold, encounter is trivial (0.7 = 70%)")]
        [Range(0f, 1f)]
        [SerializeField] private float trivialEncounterHPRatio = 0.7f;

        // Public properties
        public int UntrainedToFamiliarHits => testingMode ? 1 : untrainedToFamiliarHits;
        public int FamiliarToTrainedOutcomes => testingMode ? 1 : familiarToTrainedOutcomes;
        public int TrainedToCompetentOutcomes => testingMode ? 1 : trainedToCompetentOutcomes;
        public int CompetentToProficientOutcomes => testingMode ? 1 : competentToProficientOutcomes;
        public int ProficientToAdvancedOutcomes => testingMode ? 1 : proficientToAdvancedOutcomes;
        public int AdvancedToExpertOutcomes => testingMode ? 1 : advancedToExpertOutcomes;
        public int AdvancedToExpertCrits => testingMode ? 1 : advancedToExpertCrits;
        public int ExpertToMasterOutcomes => testingMode ? 1 : expertToMasterOutcomes;
        public int ExpertToMasterCrits => testingMode ? 1 : expertToMasterCrits;
        public int MasterToGrandmasterOutcomes => testingMode ? 1 : masterToGrandmasterOutcomes;
        public int MasterToGrandmasterCrits => testingMode ? 1 : masterToGrandmasterCrits;
        public int GrandmasterToLegendaryOutcomes => testingMode ? 1 : grandmasterToLegendaryOutcomes;
        public int GrandmasterToLegendaryCrits => testingMode ? 1 : grandmasterToLegendaryCrits;
        public float MeaningfulEncounterMinHPRatio => meaningfulEncounterMinHPRatio;
        public float TrivialEncounterAttackRatio => trivialEncounterAttackRatio;
        public float TrivialEncounterHPRatio => trivialEncounterHPRatio;
        public bool TestingMode => testingMode;

        // Singleton instance (set via Resources or direct reference)
        private static ProficiencySettings _instance;
        public static ProficiencySettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<ProficiencySettings>("ProficiencySettings");
                    if (_instance == null)
                    {
                        Debug.LogWarning("ProficiencySettings instance not found! Using default values. Create one via Assets > Create > Riftbourne > Proficiency Settings and place it in a Resources folder.");
                        // Create a temporary instance with default values
                        _instance = CreateInstance<ProficiencySettings>();
                    }
                }
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }
    }
}
