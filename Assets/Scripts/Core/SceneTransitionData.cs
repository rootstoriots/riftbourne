using UnityEngine;
using Riftbourne.Characters;
using Riftbourne.Combat;
using System.Collections.Generic;

namespace Riftbourne.Core
{
    /// <summary>
    /// Container for data passed between scenes.
    /// Uses DontDestroyOnLoad pattern to persist across scene transitions.
    /// </summary>
    public class SceneTransitionData : MonoBehaviour
    {
        public static SceneTransitionData Instance { get; private set; }

        [Header("Party Data")]
        [SerializeField] private List<CharacterState> partyData = new List<CharacterState>();

        [Header("Scene Information")]
        [SerializeField] private string returnSceneName = "";
        [SerializeField] private string battleSceneName = "BattleScene";

        [Header("Battle Information")]
        [SerializeField] private Vector3 battleStartPosition = Vector3.zero;
        [Tooltip("Encounter data for the battle (optional - if null, CombatInitiator will use Inspector-assigned encounter)")]
        [SerializeField] private EncounterData encounterData;
        
        [Header("Exploration Position")]
        [Tooltip("Player position in exploration scene before battle")]
        [SerializeField] private Vector3 explorationPosition = Vector3.zero;

        // Public properties
        public List<CharacterState> PartyData
        {
            get => partyData;
            set => partyData = value != null ? new List<CharacterState>(value) : new List<CharacterState>();
        }

        public string ReturnSceneName
        {
            get => returnSceneName;
            set => returnSceneName = value;
        }

        public string BattleSceneName
        {
            get => battleSceneName;
            set => battleSceneName = value;
        }

        public Vector3 BattleStartPosition
        {
            get => battleStartPosition;
            set => battleStartPosition = value;
        }

        public Vector3 ExplorationPosition
        {
            get => explorationPosition;
            set => explorationPosition = value;
        }

        public EncounterData EncounterData
        {
            get => encounterData;
            set => encounterData = value;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Clear all transition data.
        /// </summary>
        public void Clear()
        {
            partyData.Clear();
            returnSceneName = "";
            battleStartPosition = Vector3.zero;
            explorationPosition = Vector3.zero;
            encounterData = null;
        }
    }
}
