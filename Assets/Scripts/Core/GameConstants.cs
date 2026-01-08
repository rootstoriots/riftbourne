using UnityEngine;

namespace Riftbourne.Core
{
    /// <summary>
    /// Centralized game configuration values.
    /// Create an instance via Assets > Create > Riftbourne > GameConstants
    /// </summary>
    [CreateAssetMenu(fileName = "GameConstants", menuName = "Riftbourne/GameConstants")]
    public class GameConstants : ScriptableObject
    {
        [Header("XP System")]
        [Tooltip("Base XP awarded for performing an action (attack, skill use)")]
        [SerializeField] private int baseActionXP = 5;
        
        [Tooltip("Bonus XP awarded for killing an enemy")]
        [SerializeField] private int killBonusXP = 25;

        [Header("Skill Point (SP) System")]
        [Tooltip("Number of actions required to earn 1 Skill Point")]
        [SerializeField] private int actionsPerSP = 5;

        [Header("Leveling System")]
        [Tooltip("Number of stat points allocated per level up")]
        [SerializeField] private int statsPerLevel = 2;

        [Header("Combat System")]
        [Tooltip("Minimum damage that can be dealt (damage cannot be reduced below this)")]
        [SerializeField] private int minimumDamage = 1;

        // Public properties
        public int BaseActionXP => baseActionXP;
        public int KillBonusXP => killBonusXP;
        public int ActionsPerSP => actionsPerSP;
        public int StatsPerLevel => statsPerLevel;
        public int MinimumDamage => minimumDamage;

        // Singleton instance (set via Resources or direct reference)
        private static GameConstants _instance;
        public static GameConstants Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<GameConstants>("GameConstants");
                    if (_instance == null)
                    {
                        Debug.LogError("GameConstants instance not found! Create one via Assets > Create > Riftbourne > GameConstants and place it in a Resources folder.");
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

