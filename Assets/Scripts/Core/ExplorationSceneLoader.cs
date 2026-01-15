using UnityEngine;
using UnityEngine.SceneManagement;
using Riftbourne.Characters;
using System.Collections.Generic;
using System.Linq;

namespace Riftbourne.Core
{
    /// <summary>
    /// Handles transition from battle to exploration.
    /// Receives updated character data from battle and updates PartyManager.
    /// </summary>
    public class ExplorationSceneLoader : MonoBehaviour
    {
        [Header("Scene Names")]
        [SerializeField] private string defaultExplorationScene = "ExplorationScene";

        /// <summary>
        /// Update party from battle Units and load exploration scene.
        /// </summary>
        public void LoadExplorationScene(string sceneName = null)
        {
            if (PartyManager.Instance == null)
            {
                Debug.LogError("ExplorationSceneLoader: PartyManager not available!");
                return;
            }

            // Get all player Units from battle
            List<Unit> battleUnits = PartyManager.Instance.GetPartyMembersAsUnits();
            if (battleUnits != null && battleUnits.Count > 0)
            {
                UpdatePartyFromBattle(battleUnits);
            }

            // Load exploration scene
            string targetScene = sceneName ?? defaultExplorationScene;
            if (SceneTransitionData.Instance != null)
            {
                string returnScene = SceneTransitionData.Instance.ReturnSceneName;
                if (!string.IsNullOrEmpty(returnScene))
                {
                    targetScene = returnScene;
                }
            }

            Debug.Log($"ExplorationSceneLoader: Loading exploration scene {targetScene}");
            SceneManager.LoadScene(targetScene);
        }

        /// <summary>
        /// Update party CharacterStates from battle Units.
        /// </summary>
        public void UpdatePartyFromBattle(List<Unit> battleUnits)
        {
            if (battleUnits == null || battleUnits.Count == 0)
            {
                Debug.LogWarning("ExplorationSceneLoader: No battle units provided!");
                return;
            }

            if (PartyManager.Instance == null)
            {
                Debug.LogError("ExplorationSceneLoader: PartyManager not available!");
                return;
            }

            List<CharacterState> partyMembers = PartyManager.Instance.GetPartyMembers();

            // Update each CharacterState from corresponding Unit
            foreach (Unit unit in battleUnits)
            {
                if (unit == null) continue;

                // Find matching CharacterState by name or ID
                CharacterState matchingState = null;
                foreach (var state in partyMembers)
                {
                    if (state.Definition != null && state.Definition.CharacterName == unit.UnitName)
                    {
                        matchingState = state;
                        break;
                    }
                }

                if (matchingState != null)
                {
                    // Update CharacterState from Unit
                    matchingState.UpdateFromUnit(unit);
                    
                    // Sync inventory from Unit to CharacterState
                    unit.SyncInventoryToCharacterState();
                    
                    Debug.Log($"ExplorationSceneLoader: Updated {matchingState.CharacterID} from battle Unit {unit.UnitName}");
                }
                else
                {
                    Debug.LogWarning($"ExplorationSceneLoader: No matching CharacterState found for Unit {unit.UnitName}");
                }
            }

            Debug.Log($"ExplorationSceneLoader: Updated {partyMembers.Count} party members from battle");
        }

        /// <summary>
        /// Load exploration scene with updated party data.
        /// </summary>
        public void LoadExplorationScene(List<CharacterState> updatedParty, string sceneName = null)
        {
            if (updatedParty == null || updatedParty.Count == 0)
            {
                Debug.LogWarning("ExplorationSceneLoader: No party data provided!");
                return;
            }

            if (PartyManager.Instance == null)
            {
                Debug.LogError("ExplorationSceneLoader: PartyManager not available!");
                return;
            }

            // Update PartyManager with new party data
            PartyManager.Instance.ClearParty();
            foreach (var character in updatedParty)
            {
                PartyManager.Instance.AddPartyMember(character);
            }

            // Load exploration scene
            string targetScene = sceneName ?? defaultExplorationScene;
            if (SceneTransitionData.Instance != null)
            {
                string returnScene = SceneTransitionData.Instance.ReturnSceneName;
                if (!string.IsNullOrEmpty(returnScene))
                {
                    targetScene = returnScene;
                }
            }

            Debug.Log($"ExplorationSceneLoader: Loading exploration scene {targetScene} with {updatedParty.Count} party members");
            // Position will be restored by ExplorationPositionRestorer in the exploration scene
            SceneManager.LoadScene(targetScene);
        }
    }
}
